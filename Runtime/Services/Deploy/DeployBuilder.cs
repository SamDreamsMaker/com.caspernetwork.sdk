using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using CasperSDK.Models;
using CasperSDK.Utilities.Cryptography;
using CasperSDK.Core.Configuration;
using Org.BouncyCastle.Crypto.Digests;

namespace CasperSDK.Services.Deploy
{
    /// <summary>
    /// Builder for creating Casper Network deploys.
    /// Implements the Builder pattern for constructing complex deploy objects.
    /// </summary>
    public class DeployBuilder
    {
        private string _senderPublicKey;
        private string _chainName = "casper-test";
        private long _gasPrice = 1;
        private long _ttl = 1800000; // 30 minutes
        private string[] _dependencies = Array.Empty<string>();
        private ExecutableDeployItem _payment;
        private ExecutableDeployItem _session;
        private DateTime? _timestamp;

        /// <summary>
        /// Sets the sender's public key
        /// </summary>
        public DeployBuilder SetSender(string publicKey)
        {
            if (string.IsNullOrWhiteSpace(publicKey))
                throw new ArgumentException("Sender public key cannot be null or empty");
            
            _senderPublicKey = publicKey;
            return this;
        }

        /// <summary>
        /// Sets the chain name (default: casper-test)
        /// </summary>
        public DeployBuilder SetChainName(string chainName)
        {
            if (string.IsNullOrWhiteSpace(chainName))
                throw new ArgumentException("Chain name cannot be null or empty");
            
            _chainName = chainName;
            return this;
        }

        /// <summary>
        /// Sets the gas price (default: 1)
        /// </summary>
        public DeployBuilder SetGasPrice(long gasPrice)
        {
            if (gasPrice <= 0)
                throw new ArgumentException("Gas price must be positive");
            
            _gasPrice = gasPrice;
            return this;
        }

        /// <summary>
        /// Sets the time-to-live in milliseconds (default: 30 minutes)
        /// </summary>
        public DeployBuilder SetTTL(long ttlMs)
        {
            if (ttlMs <= 0)
                throw new ArgumentException("TTL must be positive");
            
            _ttl = ttlMs;
            return this;
        }

        /// <summary>
        /// Sets the timestamp (default: now)
        /// </summary>
        public DeployBuilder SetTimestamp(DateTime timestamp)
        {
            _timestamp = timestamp;
            return this;
        }

        /// <summary>
        /// Sets deploy dependencies
        /// </summary>
        public DeployBuilder SetDependencies(string[] deployHashes)
        {
            _dependencies = deployHashes ?? Array.Empty<string>();
            return this;
        }

        /// <summary>
        /// Sets the payment (standard payment with gas amount)
        /// </summary>
        public DeployBuilder SetStandardPayment(string amount)
        {
            if (string.IsNullOrWhiteSpace(amount))
                throw new ArgumentException("Payment amount cannot be null or empty");

            _payment = new ExecutableDeployItem
            {
                Type = "ModuleBytes",
                ModuleBytes = "", // Empty for standard payment
                Args = new[]
                {
                    new RuntimeArg
                    {
                        Name = "amount",
                        Value = CLValueBuilder.U512(amount)
                    }
                }
            };
            return this;
        }

        /// <summary>
        /// Sets the session as a native transfer
        /// </summary>
        public DeployBuilder SetTransferSession(string targetPublicKey, string amount, ulong? transferId = null)
        {
            if (string.IsNullOrWhiteSpace(targetPublicKey))
                throw new ArgumentException("Target public key cannot be null or empty");
            if (string.IsNullOrWhiteSpace(amount))
                throw new ArgumentException("Amount cannot be null or empty");

            var args = new List<RuntimeArg>
            {
                new RuntimeArg { Name = "amount", Value = CLValueBuilder.U512(amount) },
                new RuntimeArg { Name = "target", Value = CLValueBuilder.PublicKey(targetPublicKey) }
            };

            if (transferId.HasValue)
            {
                args.Add(new RuntimeArg { Name = "id", Value = CLValueBuilder.OptionU64(transferId.Value) });
            }
            else
            {
                args.Add(new RuntimeArg { Name = "id", Value = CLValueBuilder.OptionNone() });
            }

            _session = new ExecutableDeployItem
            {
                Type = "Transfer",
                Args = args.ToArray()
            };
            return this;
        }

        /// <summary>
        /// Sets the session as a stored contract call
        /// </summary>
        public DeployBuilder SetContractSession(string contractHash, string entryPoint, RuntimeArg[] args)
        {
            _session = new ExecutableDeployItem
            {
                Type = "StoredContractByHash",
                ContractHash = contractHash,
                EntryPoint = entryPoint,
                Args = args ?? Array.Empty<RuntimeArg>()
            };
            return this;
        }

        /// <summary>
        /// Sets the session as WASM module bytes
        /// </summary>
        public DeployBuilder SetWasmSession(byte[] wasmBytes, RuntimeArg[] args)
        {
            _session = new ExecutableDeployItem
            {
                Type = "ModuleBytes",
                ModuleBytes = CryptoHelper.BytesToHex(wasmBytes),
                Args = args ?? Array.Empty<RuntimeArg>()
            };
            return this;
        }

        /// <summary>
        /// Builds the deploy without signing
        /// </summary>
        public Models.Deploy Build()
        {
            Validate();

            // Subtract 30 seconds to avoid "timestamp in future" errors from clock drift with nodes
            var timestamp = (_timestamp ?? DateTime.UtcNow).AddSeconds(-30);
            var timestampStr = timestamp.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");

            // Calculate body hash
            var bodyHash = CalculateBodyHash(_payment, _session);

            // Create header
            var header = new DeployHeader
            {
                Account = _senderPublicKey,
                Timestamp = timestampStr,
                TTL = _ttl,
                GasPrice = _gasPrice,
                BodyHash = bodyHash,
                Dependencies = _dependencies,
                ChainName = _chainName
            };

            // Calculate deploy hash from header
            var deployHash = CalculateDeployHash(header);

            return new Models.Deploy
            {
                Hash = deployHash,
                Header = header,
                Payment = _payment,
                Session = _session,
                Approvals = Array.Empty<DeployApproval>()
            };
        }

        private void Validate()
        {
            if (string.IsNullOrWhiteSpace(_senderPublicKey))
                throw new InvalidOperationException("Sender public key is required");
            if (_payment == null)
                throw new InvalidOperationException("Payment is required");
            if (_session == null)
                throw new InvalidOperationException("Session is required");
        }

        /// <summary>
        /// Calculates the body hash (Blake2b-256 of serialized payment + session)
        /// </summary>
        private string CalculateBodyHash(ExecutableDeployItem payment, ExecutableDeployItem session)
        {
            // Serialize payment and session
            var paymentBytes = SerializeExecutableDeployItem(payment);
            var sessionBytes = SerializeExecutableDeployItem(session);

            // Combine
            var combined = new byte[paymentBytes.Length + sessionBytes.Length];
            Buffer.BlockCopy(paymentBytes, 0, combined, 0, paymentBytes.Length);
            Buffer.BlockCopy(sessionBytes, 0, combined, paymentBytes.Length, sessionBytes.Length);

            // Hash with Blake2b-256
            return HashBlake2b256(combined);
        }

        /// <summary>
        /// Calculates the deploy hash (Blake2b-256 of serialized header)
        /// </summary>
        private string CalculateDeployHash(DeployHeader header)
        {
            var headerBytes = SerializeDeployHeader(header);
            return HashBlake2b256(headerBytes);
        }

        /// <summary>
        /// Computes Blake2b-256 hash
        /// </summary>
        private string HashBlake2b256(byte[] data)
        {
            var blake2b = new Blake2bDigest(256);
            blake2b.BlockUpdate(data, 0, data.Length);
            var hash = new byte[32];
            blake2b.DoFinal(hash, 0);
            return CryptoHelper.BytesToHex(hash);
        }

        /// <summary>
        /// Serializes a deploy header to bytes according to Casper bytesrepr format
        /// </summary>
        private byte[] SerializeDeployHeader(DeployHeader header)
        {
            var parts = new List<byte>();

            // Account (public key: just the bytes, no length prefix)
            // PublicKey format: [algo_tag(1 byte)][key_bytes(32 or 33 bytes)]
            var accountBytes = CryptoHelper.HexToBytes(header.Account);
            parts.AddRange(accountBytes);

            // Timestamp (as milliseconds since epoch, u64 LE)
            var timestamp = DateTime.Parse(header.Timestamp).ToUniversalTime();
            var epochMs = (long)(timestamp - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds;
            parts.AddRange(BitConverter.GetBytes((ulong)epochMs));

            // TTL (u64 LE)
            parts.AddRange(BitConverter.GetBytes((ulong)header.TTL));

            // Gas price (u64 LE) 
            parts.AddRange(BitConverter.GetBytes((ulong)header.GasPrice));

            // Body hash (32 bytes, no length prefix)
            var bodyHashBytes = CryptoHelper.HexToBytes(header.BodyHash);
            parts.AddRange(bodyHashBytes);

            // Dependencies count (u32 LE) and hashes
            parts.AddRange(BitConverter.GetBytes((uint)header.Dependencies.Length));
            foreach (var dep in header.Dependencies)
            {
                parts.AddRange(CryptoHelper.HexToBytes(dep));
            }

            // Chain name (length-prefixed string, u32 LE)
            var chainNameBytes = Encoding.UTF8.GetBytes(header.ChainName);
            parts.AddRange(BitConverter.GetBytes((uint)chainNameBytes.Length));
            parts.AddRange(chainNameBytes);

            return parts.ToArray();
        }

        /// <summary>
        /// Serializes an executable deploy item to bytes
        /// </summary>
        private byte[] SerializeExecutableDeployItem(ExecutableDeployItem item)
        {
            var parts = new List<byte>();

            // Type tag
            byte typeTag = item.Type switch
            {
                "ModuleBytes" => 0,
                "StoredContractByHash" => 1,
                "StoredContractByName" => 2,
                "StoredVersionedContractByHash" => 3,
                "StoredVersionedContractByName" => 4,
                "Transfer" => 5,
                _ => 0
            };
            parts.Add(typeTag);

            // Serialize based on type
            switch (item.Type)
            {
                case "ModuleBytes":
                    var moduleBytes = string.IsNullOrEmpty(item.ModuleBytes) 
                        ? Array.Empty<byte>() 
                        : CryptoHelper.HexToBytes(item.ModuleBytes);
                    parts.AddRange(BitConverter.GetBytes((uint)moduleBytes.Length));
                    parts.AddRange(moduleBytes);
                    parts.AddRange(SerializeRuntimeArgs(item.Args));
                    break;

                case "Transfer":
                    parts.AddRange(SerializeRuntimeArgs(item.Args));
                    break;

                case "StoredContractByHash":
                    var contractBytes = CryptoHelper.HexToBytes(item.ContractHash);
                    parts.AddRange(contractBytes);
                    var epBytes = Encoding.UTF8.GetBytes(item.EntryPoint ?? "");
                    parts.AddRange(BitConverter.GetBytes((uint)epBytes.Length));
                    parts.AddRange(epBytes);
                    parts.AddRange(SerializeRuntimeArgs(item.Args));
                    break;

                default:
                    // Simplified for other types
                    parts.AddRange(SerializeRuntimeArgs(item.Args));
                    break;
            }

            return parts.ToArray();
        }

        /// <summary>
        /// Serializes runtime arguments according to Casper bytesrepr format
        /// Format: [count][arg1][arg2]...
        /// Each arg: [name_len][name][value_bytes_len][value_bytes][cltype_bytes]
        /// </summary>
        private byte[] SerializeRuntimeArgs(RuntimeArg[] args)
        {
            var parts = new List<byte>();
            args = args ?? Array.Empty<RuntimeArg>();

            // Args count (u32 LE)
            parts.AddRange(BitConverter.GetBytes((uint)args.Length));

            foreach (var arg in args)
            {
                // Name (length-prefixed string)
                var nameBytes = Encoding.UTF8.GetBytes(arg.Name ?? "");
                parts.AddRange(BitConverter.GetBytes((uint)nameBytes.Length));
                parts.AddRange(nameBytes);

                // CLValue: [bytes_len][bytes][cltype_serialized]
                var valueBytes = CryptoHelper.HexToBytes(arg.Value?.Bytes ?? "");
                var clTypeBytes = SerializeCLType(arg.Value?.CLType ?? "Unit");
                
                // Value bytes length + bytes
                parts.AddRange(BitConverter.GetBytes((uint)valueBytes.Length));
                parts.AddRange(valueBytes);
                
                // CLType (no length prefix, directly appended)
                parts.AddRange(clTypeBytes);
            }

            return parts.ToArray();
        }

        /// <summary>
        /// Serializes CLType to bytes according to Casper spec
        /// </summary>
        private byte[] SerializeCLType(string clType)
        {
            // Simple types have a single byte tag
            switch (clType)
            {
                case "Bool": return new byte[] { 0 };
                case "I32": return new byte[] { 1 };
                case "I64": return new byte[] { 2 };
                case "U8": return new byte[] { 3 };
                case "U32": return new byte[] { 4 };
                case "U64": return new byte[] { 5 };
                case "U128": return new byte[] { 6 };
                case "U256": return new byte[] { 7 };
                case "U512": return new byte[] { 8 };
                case "Unit": return new byte[] { 9 };
                case "String": return new byte[] { 10 };
                case "Key": return new byte[] { 11 };
                case "URef": return new byte[] { 12 };
                case "PublicKey": return new byte[] { 22 };
            }

            // Option type: tag 13 + inner type
            if (clType == "Option")
            {
                return new byte[] { 13, 5 }; // Option(U64) for transfer id
            }
            if (clType.StartsWith("Option(") && clType.EndsWith(")"))
            {
                var inner = clType.Substring(7, clType.Length - 8);
                var innerBytes = SerializeCLType(inner);
                var result = new byte[1 + innerBytes.Length];
                result[0] = 13; // Option tag
                Buffer.BlockCopy(innerBytes, 0, result, 1, innerBytes.Length);
                return result;
            }

            // List type: tag 14 + inner type
            if (clType.StartsWith("List(") && clType.EndsWith(")"))
            {
                var inner = clType.Substring(5, clType.Length - 6);
                var innerBytes = SerializeCLType(inner);
                var result = new byte[1 + innerBytes.Length];
                result[0] = 14; // List tag
                Buffer.BlockCopy(innerBytes, 0, result, 1, innerBytes.Length);
                return result;
            }

            // ByteArray - tag 15 + size (u32)
            if (clType.StartsWith("ByteArray(") && clType.EndsWith(")"))
            {
                var sizeStr = clType.Substring(10, clType.Length - 11);
                var size = uint.Parse(sizeStr);
                var result = new byte[5];
                result[0] = 15; // ByteArray tag
                Buffer.BlockCopy(BitConverter.GetBytes(size), 0, result, 1, 4);
                return result;
            }

            // Map type: tag 17 + key type + value type
            if (clType.StartsWith("Map(") && clType.EndsWith(")"))
            {
                var inner = clType.Substring(4, clType.Length - 5);
                var comma = inner.IndexOf(',');
                if (comma > 0)
                {
                    var keyType = inner.Substring(0, comma).Trim();
                    var valueType = inner.Substring(comma + 1).Trim();
                    var keyBytes = SerializeCLType(keyType);
                    var valueBytes = SerializeCLType(valueType);
                    var result = new byte[1 + keyBytes.Length + valueBytes.Length];
                    result[0] = 17; // Map tag
                    Buffer.BlockCopy(keyBytes, 0, result, 1, keyBytes.Length);
                    Buffer.BlockCopy(valueBytes, 0, result, 1 + keyBytes.Length, valueBytes.Length);
                    return result;
                }
            }

            // Default: Unit
            return new byte[] { 9 };
        }
    }
}
