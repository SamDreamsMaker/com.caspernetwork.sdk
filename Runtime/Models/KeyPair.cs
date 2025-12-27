namespace CasperSDK.Models
{
    /// <summary>
    /// Represents a cryptographic key pair
    /// </summary>
    public class KeyPair
    {
        /// <summary>
        /// Public key in hexadecimal format
        /// </summary>
        public string PublicKeyHex { get; set; }

        /// <summary>
        /// Private key in hexadecimal format (should be kept secure)
        /// </summary>
        public string PrivateKeyHex { get; set; }

        /// <summary>
        /// Key algorithm used
        /// </summary>
        public KeyAlgorithm Algorithm { get; set; }

        /// <summary>
        /// Account hash derived from public key
        /// </summary>
        public string AccountHash { get; set; }
    }
}
