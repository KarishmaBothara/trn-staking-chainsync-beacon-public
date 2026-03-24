using System;
using System.Text;
using Isopoh.Cryptography.Blake2b;
using Isopoh.Cryptography.SecureArray;

namespace MatrixEngine.Core.Substrate.Ledger
{
    public static class Hasher
    {
        private static byte[] Blake2_128Concat(byte[] input)
        {
            var config = new Blake2BConfig { OutputSizeInBytes = 16 };
            var hash = Blake2B.ComputeHash(input, config, null);
            return hash;
        }
        
        // Compute the storage value for a given fixed prefix.
        // This will hash the key, and append the hash and key to the fixed prefix.
        public static byte[] ComputeStorageValue(string fixedPrefix, string key)
        {
            var keyBytes = Convert.FromHexString(key.Substring(2));
            var prefixBytes = Convert.FromHexString(fixedPrefix.Substring(2));
            var keyHash = Blake2_128Concat(keyBytes);
            
            // Combine fixed prefix, key hash and key value into a single byte array
            var combined = new byte[prefixBytes.Length + keyHash.Length + keyBytes.Length];
            Buffer.BlockCopy(prefixBytes, 0, combined, 0, prefixBytes.Length);
            Buffer.BlockCopy(keyHash, 0, combined, prefixBytes.Length, keyHash.Length);
            Buffer.BlockCopy(keyBytes, 0, combined, prefixBytes.Length + keyHash.Length, keyBytes.Length);
            return combined;
        }
        
        // Helper function to convert a byte array to a hex string
        public static string ToHex(byte[] bytes) =>
            "0x" + BitConverter.ToString(bytes).Replace("-", "").ToLower();
    }
}