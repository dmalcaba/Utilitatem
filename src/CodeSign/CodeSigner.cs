using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Linq;
using System.Management.Automation;

namespace CodeSign
{
    public class CodeSigner
    {
        private static void RunCodeSign(List<string> executablesList)
        {
            const string timeStampServer = @"http://tsa.starfieldtech.com";

            string certLocation = ConfigurationManager.AppSettings["CertLocation"];

            using (var powerShellInstance = PowerShell.Create())
            {
                Console.Write("Looking for code-signing certificate in ");
                //Message.Highlight($"[Cert:{certLocation}].\n");

                var psCertOutput = powerShellInstance.AddCommand("Get-ChildItem")
                    .AddParameter("Path", $"Cert:{certLocation}")
                    .AddParameter("CodeSigningCert").Invoke().FirstOrDefault();

                if (psCertOutput == null)
                {
                    Console.WriteLine("Unable to find code-signing certificate");
                    return;
                }

                var certInfo = psCertOutput.BaseObject as System.Security.Cryptography.X509Certificates.X509Certificate2;

                if (certInfo == null)
                {
                    Console.WriteLine("No certificate info available");
                    return;
                }

                Console.WriteLine("Certificate Found");
                Console.WriteLine($"Subject     : {certInfo.Subject}");
                Console.WriteLine($"Issuer      : {certInfo.Issuer}");
                Console.WriteLine($"Thumbprint  : {certInfo.Thumbprint}");
                Console.WriteLine($"Issued Date : {certInfo.NotBefore}");
                Console.WriteLine($"Expiry Date : {certInfo.NotAfter}\n");

                foreach (var execFiles in executablesList)
                {
                    powerShellInstance.Commands.Clear();

                    Collection<PSObject> pSOutput = powerShellInstance.AddCommand("Set-AuthenticodeSignature")
                        .AddParameter("FilePath", execFiles)
                        .AddParameter("Certificate", certInfo)
                        .AddParameter("TimestampServer", timeStampServer).Invoke();

                    foreach (var psObject in pSOutput)
                    {
                        var signature = psObject.BaseObject as Signature;

                        if (signature != null)
                        {
                            var signatureStatus = signature.Status;

                            if (signatureStatus == SignatureStatus.Valid)
                            {
                                //Message.Success($"{execFiles} is code-signed successfully. {signature.StatusMessage}");
                            }
                            else
                            {
                                //Message.Error($"Signing {execFiles} unsuccessful. {signature.StatusMessage}");
                            }

                            Console.WriteLine();
                        }
                        else
                        {
                            Console.WriteLine(psObject);
                        }
                    }

                    // if any errors
                    if (powerShellInstance.Streams.Error.Count > 0)
                    {
                        foreach (var errorRecord in powerShellInstance.Streams.Error)
                        {
                            Console.WriteLine(errorRecord.Exception.Message);
                        }
                    }
                }
            }
        }

    }
}
