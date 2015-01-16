using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Management.Automation;
using System.Security;
using System.Security.Cryptography;

namespace HSR.AzureEE.Module
{
    [Cmdlet(VerbsCommon.Get, "EncryptedPassword")]
    public class GetEncryptedPassword : Cmdlet
    {
        // Declare the parameters for the cmdlet.
        [Parameter(Mandatory = true)]
        [ValidateNotNullOrEmpty]
        public SecureString Password
        {
            get { return _encryptedPassword; }
            set { _encryptedPassword = value; }
        }

        private SecureString _encryptedPassword;

        protected override void ProcessRecord()
        {
            WriteObject(EncryptString(_encryptedPassword));

            // test
            //string blah = EncryptString(_encryptedPassword);
            //SecureString securePwd = DecryptString(blah);
            //Console.Write(ToInsecureString(securePwd));
            //Console.WriteLine();
        }

        protected override void StopProcessing()
        {
            Console.WriteLine();
        }

        static byte[] entropy = System.Text.Encoding.Unicode.GetBytes("1A36b85c3t9d09h1r");

        public static string EncryptString(SecureString input)
        {
            byte[] encryptedData = ProtectedData.Protect(
                System.Text.Encoding.Unicode.GetBytes(ToInsecureString(input)),
                entropy,
                DataProtectionScope.CurrentUser);
            return Convert.ToBase64String(encryptedData);
        }

        public static SecureString DecryptString(string encryptedData)
        {
            try
            {
                byte[] decryptedData = ProtectedData.Unprotect(
                    Convert.FromBase64String(encryptedData),
                    entropy,
                    DataProtectionScope.CurrentUser);
                return ToSecureString(System.Text.Encoding.Unicode.GetString(decryptedData));
            }
            catch
            {
                return new SecureString();
            }
        }

        public static SecureString ToSecureString(string input)
        {
            SecureString secure = new SecureString();
            foreach (char c in input)
            {
                secure.AppendChar(c);
            }
            secure.MakeReadOnly();
            return secure;
        }

        public static string ToInsecureString(SecureString input)
        {
            string returnValue = string.Empty;
            IntPtr ptr = System.Runtime.InteropServices.Marshal.SecureStringToBSTR(input);

            try
            {
                returnValue = System.Runtime.InteropServices.Marshal.PtrToStringBSTR(ptr);
            }
            finally
            {
                System.Runtime.InteropServices.Marshal.ZeroFreeBSTR(ptr);
            }

            return returnValue;
        }
    }
}
