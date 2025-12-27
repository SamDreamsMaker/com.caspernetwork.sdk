using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using CasperSDK.Core.Configuration;
using CasperSDK.Core.Interfaces;
using CasperSDK.Models.RPC;

namespace CasperSDK.Services.Validator
{
    /// <summary>
    /// Service for querying validator and staking information.
    /// </summary>
    public class ValidatorService : IValidatorService
    {
        private readonly INetworkClient _networkClient;
        private readonly bool _enableLogging;

        public ValidatorService(INetworkClient networkClient, NetworkConfig config)
        {
            _networkClient = networkClient ?? throw new ArgumentNullException(nameof(networkClient));
            _enableLogging = config.EnableLogging;
        }

        /// <inheritdoc/>
        public async Task<AuctionInfo> GetAuctionInfoAsync(string blockHash = null)
        {
            try
            {
                if (_enableLogging)
                {
                    Debug.Log("[CasperSDK] Getting auction info");
                }

                object param = null;
                if (!string.IsNullOrEmpty(blockHash))
                {
                    param = new { block_identifier = new { Hash = blockHash } };
                }

                var result = await _networkClient.SendRequestAsync<AuctionInfoResponse>("state_get_auction_info", param);

                if (result?.auction_state == null)
                {
                    return null;
                }

                var auctionState = result.auction_state;

                // Convert era validators
                var eraValidators = new List<EraValidatorInfo>();
                if (auctionState.era_validators != null)
                {
                    foreach (var era in auctionState.era_validators)
                    {
                        var validators = new List<ValidatorInfo>();
                        if (era.validator_weights != null)
                        {
                            foreach (var v in era.validator_weights)
                            {
                                validators.Add(new ValidatorInfo
                                {
                                    PublicKey = v.public_key,
                                    Weight = v.weight,
                                    EraId = era.era_id
                                });
                            }
                        }
                        eraValidators.Add(new EraValidatorInfo
                        {
                            EraId = era.era_id,
                            Validators = validators.ToArray()
                        });
                    }
                }

                // Convert bids
                var bids = new List<ValidatorBid>();
                if (auctionState.bids != null)
                {
                    foreach (var bid in auctionState.bids)
                    {
                        bids.Add(new ValidatorBid
                        {
                            PublicKey = bid.public_key,
                            BondingPurse = bid.bid?.bonding_purse,
                            StakedAmount = bid.bid?.staked_amount,
                            DelegationRate = bid.bid?.delegation_rate ?? 0,
                            Inactive = bid.bid?.inactive ?? false
                        });
                    }
                }

                return new AuctionInfo
                {
                    StateRootHash = auctionState.state_root_hash,
                    BlockHeight = auctionState.block_height,
                    EraValidators = eraValidators.ToArray(),
                    Bids = bids.ToArray()
                };
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CasperSDK] Failed to get auction info: {ex.Message}");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<ValidatorInfo[]> GetValidatorsAsync()
        {
            try
            {
                if (_enableLogging)
                {
                    Debug.Log("[CasperSDK] Getting validators list");
                }

                var auctionInfo = await GetAuctionInfoAsync();

                if (auctionInfo?.EraValidators == null || auctionInfo.EraValidators.Length == 0)
                {
                    return new ValidatorInfo[0];
                }

                // Return validators from the current era (first one)
                return auctionInfo.EraValidators[0].Validators ?? new ValidatorInfo[0];
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CasperSDK] Failed to get validators: {ex.Message}");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<ValidatorBid> GetValidatorByKeyAsync(string publicKey)
        {
            if (string.IsNullOrWhiteSpace(publicKey))
            {
                throw new ArgumentException("Public key cannot be null or empty", nameof(publicKey));
            }

            try
            {
                if (_enableLogging)
                {
                    Debug.Log($"[CasperSDK] Getting validator: {publicKey}");
                }

                var auctionInfo = await GetAuctionInfoAsync();

                if (auctionInfo?.Bids == null)
                {
                    return null;
                }

                foreach (var bid in auctionInfo.Bids)
                {
                    if (bid.PublicKey == publicKey)
                    {
                        return bid;
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CasperSDK] Failed to get validator by key: {ex.Message}");
                throw;
            }
        }
    }
}
