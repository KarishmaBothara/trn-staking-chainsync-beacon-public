using System;
using System.Collections.Generic;
using System.Numerics;
using MatrixEngine.Core.Models.DTOs;
using Microsoft.Extensions.Logging;

namespace MatrixEngine.Core.Substrate.Ledger
{
    // Utility for parsing staking ledger data from TRN node
    public static class StakingLedgerParser
    {
        // Parses the hexadecimal SCALE-encoded staking ledger data
        public static StakingLedgerModel? Parse(string hexData, ILogger logger)
        {
            if (string.IsNullOrEmpty(hexData))
            {
                logger.LogWarning("Empty hex data provided for staking ledger parsing");
                return null;
            }
        
            try
            {
                byte[] bytes = Convert.FromHexString(hexData.Substring(2));
                
                // Initialize the staking ledger model
                var ledgerModel = new StakingLedgerModel();
                
                // Create a scale decoder for parsing
                int offset = 0;
                
                // Parse stash account ID
                byte[] stashBytes = new byte[20];
                Array.Copy(bytes, offset, stashBytes, 0, 20);
                ledgerModel.Stash = Hasher.ToHex(stashBytes);
                offset += 20;
                
                // Parse total (compact encoded)
                (BigInteger total, int totalSize) = ParseCompactBigInteger(bytes, offset);
                ledgerModel.Total = total;
                offset += totalSize;
                
                // Parse active (compact encoded)
                (BigInteger active, int activeSize) = ParseCompactBigInteger(bytes, offset);
                ledgerModel.Active = active;
                offset += activeSize;
                
                // We don't need to get the unlock chunks or claimed rewards. All we need for our calculations are active and total
                return ledgerModel;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to parse staking ledger data: {Message}", ex.Message);
                return null;
            }
        }
        
        // Parses a compact-encoded BigInteger
        private static (BigInteger value, int bytesRead) ParseCompactBigInteger(byte[] data, int offset)
        {
            if (offset >= data.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(offset), "Offset exceeds data length");
            }
            
            // Simple implementation of compact integer decoding
            byte mode = (byte)(data[offset] & 0x03);
            BigInteger result;
            int bytesRead;
            
            switch (mode)
            {
                case 0: // single-byte mode
                    result = (BigInteger)(data[offset] >> 2);
                    bytesRead = 1;
                    break;
                    
                case 1: // two-byte mode
                    if (offset + 1 >= data.Length)
                        throw new ArgumentOutOfRangeException(nameof(offset), "Not enough data for two-byte mode");
                    
                    result = (BigInteger)(((data[offset] >> 2) | (data[offset + 1] << 6)) & 0x3FFF);
                    bytesRead = 2;
                    break;
                    
                case 2: // four-byte mode
                    if (offset + 3 >= data.Length)
                        throw new ArgumentOutOfRangeException(nameof(offset), "Not enough data for four-byte mode");
                    
                    result = (BigInteger)(
                        ((uint)data[offset] >> 2) |
                        ((uint)data[offset + 1] << 6) |
                        ((uint)data[offset + 2] << 14) |
                        ((uint)data[offset + 3] << 22)
                    );
                    bytesRead = 4;
                    break;
                    
                case 3: // Big integer mode
                    int byteLen = (data[offset] >> 2) + 4;
                    if (offset + byteLen >= data.Length)
                        throw new ArgumentOutOfRangeException(nameof(offset), "Not enough data for big integer mode");
                    
                    byte[] valueBytes = new byte[byteLen];
                    Array.Copy(data, offset + 1, valueBytes, 0, byteLen);
                    
                    // Add an extra byte to ensure the number is treated as unsigned
                    // This is needed because BigInteger in C# uses the highest bit as a sign bit
                    byte[] unsignedBytes = new byte[byteLen + 1];
                    Array.Copy(valueBytes, 0, unsignedBytes, 0, byteLen);
                    unsignedBytes[byteLen] = 0; // Ensure the number is positive
                    
                    result = new BigInteger(unsignedBytes);
                    bytesRead = 1 + byteLen;
                    break;
                    
                default:
                    throw new NotImplementedException("Unhandled compact integer mode");
            }
            
            return (result, bytesRead);
        }
    }
} 