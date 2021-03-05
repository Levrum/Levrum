using System;
using System.Collections.Generic;
using System.Text;

namespace Levrum.Utils.Infra
{
    public class StringCryptor
    {
        private String encryptionKey;

        public StringCryptor(String encryptionKey)
        {
            this.encryptionKey = encryptionKey;
        }

        public String Encrypt(String stringToEncrypt)
        {
            String encryptedString = "";

            char currentEncryptionKeyChar;

            for (int i = 0; i < stringToEncrypt.Length; ++i)
            {
                currentEncryptionKeyChar = this.encryptionKey[i % this.encryptionKey.Length];

                encryptedString += (char)((uint)stringToEncrypt[i] ^ currentEncryptionKeyChar);
            }

            return encryptedString;
        }

        public String Decrypt(String stringToDecrypt)
        {
            return Encrypt(stringToDecrypt);
        }
    }

}
