using System;
using System.Text;
using CasperSDK.Models;

namespace CasperSDK.Services.Deploy
{
    /// <summary>
    /// Builder for creating CLValues - the type system for Casper contract arguments.
    /// Supports common types: U8, U32, U64, U128, U256, U512, String, Key, URef, PublicKey, etc.
    /// </summary>
    public static class CLValueBuilder
    {
        #region Unsigned Integers

        /// <summary>
        /// Creates a U8 CLValue
        /// </summary>
        public static CLValue U8(byte value)
        {
            return new CLValue
            {
                CLType = "U8",
                Bytes = value.ToString("x2"),
                Parsed = value
            };
        }

        /// <summary>
        /// Creates a U32 CLValue
        /// </summary>
        public static CLValue U32(uint value)
        {
            var bytes = BitConverter.GetBytes(value);
            return new CLValue
            {
                CLType = "U32",
                Bytes = Utilities.Cryptography.CryptoHelper.BytesToHex(bytes),
                Parsed = value
            };
        }

        /// <summary>
        /// Creates a U64 CLValue
        /// </summary>
        public static CLValue U64(ulong value)
        {
            var bytes = BitConverter.GetBytes(value);
            return new CLValue
            {
                CLType = "U64",
                Bytes = Utilities.Cryptography.CryptoHelper.BytesToHex(bytes),
                Parsed = value
            };
        }

        /// <summary>
        /// Creates a U128 CLValue from a string (for large numbers)
        /// </summary>
        public static CLValue U128(string value)
        {
            return CreateBigNumber(value, "U128", 16);
        }

        /// <summary>
        /// Creates a U256 CLValue from a string (for large numbers)
        /// </summary>
        public static CLValue U256(string value)
        {
            return CreateBigNumber(value, "U256", 32);
        }

        /// <summary>
        /// Creates a U512 CLValue from a string (for large numbers, used for amounts)
        /// </summary>
        public static CLValue U512(string value)
        {
            return CreateBigNumber(value, "U512", 64);
        }

        private static CLValue CreateBigNumber(string value, string clType, int maxBytes)
        {
            // Parse as big integer and serialize in Casper format
            var bigInt = System.Numerics.BigInteger.Parse(value);
            var bytes = bigInt.ToByteArray();
            
            // Remove trailing zeros and create length-prefixed format
            var trimmed = TrimLeadingZeros(bytes);
            var result = new byte[trimmed.Length + 1];
            result[0] = (byte)trimmed.Length;
            Buffer.BlockCopy(trimmed, 0, result, 1, trimmed.Length);

            return new CLValue
            {
                CLType = clType,
                Bytes = Utilities.Cryptography.CryptoHelper.BytesToHex(result),
                Parsed = value
            };
        }

        private static byte[] TrimLeadingZeros(byte[] bytes)
        {
            // BigInteger uses little-endian, so trailing zeros are actually leading
            int lastNonZero = bytes.Length - 1;
            while (lastNonZero > 0 && bytes[lastNonZero] == 0)
                lastNonZero--;
            
            var result = new byte[lastNonZero + 1];
            Buffer.BlockCopy(bytes, 0, result, 0, result.Length);
            return result;
        }

        #endregion

        #region Signed Integers

        /// <summary>
        /// Creates an I32 CLValue
        /// </summary>
        public static CLValue I32(int value)
        {
            var bytes = BitConverter.GetBytes(value);
            return new CLValue
            {
                CLType = "I32",
                Bytes = Utilities.Cryptography.CryptoHelper.BytesToHex(bytes),
                Parsed = value
            };
        }

        /// <summary>
        /// Creates an I64 CLValue
        /// </summary>
        public static CLValue I64(long value)
        {
            var bytes = BitConverter.GetBytes(value);
            return new CLValue
            {
                CLType = "I64",
                Bytes = Utilities.Cryptography.CryptoHelper.BytesToHex(bytes),
                Parsed = value
            };
        }

        #endregion

        #region Boolean and String

        /// <summary>
        /// Creates a Bool CLValue
        /// </summary>
        public static CLValue Bool(bool value)
        {
            return new CLValue
            {
                CLType = "Bool",
                Bytes = value ? "01" : "00",
                Parsed = value
            };
        }

        /// <summary>
        /// Creates a String CLValue
        /// </summary>
        public static CLValue String(string value)
        {
            var stringBytes = Encoding.UTF8.GetBytes(value ?? "");
            var lengthBytes = BitConverter.GetBytes((uint)stringBytes.Length);
            
            var result = new byte[lengthBytes.Length + stringBytes.Length];
            Buffer.BlockCopy(lengthBytes, 0, result, 0, lengthBytes.Length);
            Buffer.BlockCopy(stringBytes, 0, result, lengthBytes.Length, stringBytes.Length);

            return new CLValue
            {
                CLType = "String",
                Bytes = Utilities.Cryptography.CryptoHelper.BytesToHex(result),
                Parsed = value
            };
        }

        /// <summary>
        /// Creates a Unit CLValue (empty/void)
        /// </summary>
        public static CLValue Unit()
        {
            return new CLValue
            {
                CLType = "Unit",
                Bytes = "",
                Parsed = null
            };
        }

        #endregion

        #region Keys and References

        /// <summary>
        /// Creates a PublicKey CLValue
        /// </summary>
        public static CLValue PublicKey(string publicKeyHex)
        {
            if (string.IsNullOrEmpty(publicKeyHex))
                throw new ArgumentException("Public key cannot be null or empty");

            return new CLValue
            {
                CLType = "PublicKey",
                Bytes = publicKeyHex.ToLower(),
                Parsed = publicKeyHex
            };
        }

        /// <summary>
        /// Creates a Key CLValue (generic key type)
        /// </summary>
        public static CLValue Key(string keyType, string keyHex)
        {
            // Key type prefix: 00=Account, 01=Hash, 02=URef
            byte prefix = keyType.ToLower() switch
            {
                "account" => 0x00,
                "hash" => 0x01,
                "uref" => 0x02,
                _ => throw new ArgumentException($"Unknown key type: {keyType}")
            };

            var keyBytes = Utilities.Cryptography.CryptoHelper.HexToBytes(keyHex);
            var result = new byte[1 + keyBytes.Length];
            result[0] = prefix;
            Buffer.BlockCopy(keyBytes, 0, result, 1, keyBytes.Length);

            return new CLValue
            {
                CLType = "Key",
                Bytes = Utilities.Cryptography.CryptoHelper.BytesToHex(result),
                Parsed = $"{keyType}:{keyHex}"
            };
        }

        /// <summary>
        /// Creates a URef CLValue
        /// </summary>
        public static CLValue URef(string urefHex, byte accessRights = 0x07) // 0x07 = READ_ADD_WRITE
        {
            var urefBytes = Utilities.Cryptography.CryptoHelper.HexToBytes(urefHex);
            var result = new byte[urefBytes.Length + 1];
            Buffer.BlockCopy(urefBytes, 0, result, 0, urefBytes.Length);
            result[urefBytes.Length] = accessRights;

            return new CLValue
            {
                CLType = "URef",
                Bytes = Utilities.Cryptography.CryptoHelper.BytesToHex(result),
                Parsed = $"uref-{urefHex}-{accessRights:x3}"
            };
        }

        /// <summary>
        /// Creates an AccountHash CLValue
        /// </summary>
        public static CLValue AccountHash(string accountHashHex)
        {
            // Remove "account-hash-" prefix if present
            if (accountHashHex.StartsWith("account-hash-"))
                accountHashHex = accountHashHex.Substring(13);

            return new CLValue
            {
                CLType = "AccountHash",
                Bytes = accountHashHex.ToLower(),
                Parsed = $"account-hash-{accountHashHex}"
            };
        }

        #endregion

        #region Optional Types

        /// <summary>
        /// Creates an Option(U64) CLValue
        /// </summary>
        public static CLValue OptionU64(ulong value)
        {
            var u64Bytes = BitConverter.GetBytes(value);
            var result = new byte[1 + u64Bytes.Length];
            result[0] = 0x01; // Some
            Buffer.BlockCopy(u64Bytes, 0, result, 1, u64Bytes.Length);

            return new CLValue
            {
                CLType = "Option(U64)",
                Bytes = Utilities.Cryptography.CryptoHelper.BytesToHex(result),
                Parsed = value
            };
        }

        /// <summary>
        /// Creates an Option None CLValue
        /// </summary>
        public static CLValue OptionNone()
        {
            return new CLValue
            {
                CLType = "Option",
                Bytes = "00", // None
                Parsed = null
            };
        }

        #endregion

        #region Collections

        /// <summary>
        /// Creates a ByteArray CLValue
        /// </summary>
        public static CLValue ByteArray(byte[] bytes)
        {
            return new CLValue
            {
                CLType = $"ByteArray({bytes.Length})",
                Bytes = Utilities.Cryptography.CryptoHelper.BytesToHex(bytes),
                Parsed = bytes
            };
        }

        #endregion
    }
}
