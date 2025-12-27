using System;

namespace CasperSDK.Models.RPC
{
    /// <summary>
    /// Response models for Validator/Auction RPC calls
    /// </summary>

    [Serializable]
    public class AuctionInfoResponse
    {
        public string api_version;
        public AuctionState auction_state;
    }

    [Serializable]
    public class AuctionState
    {
        public string state_root_hash;
        public long block_height;
        public EraValidators[] era_validators;
        public BidInfo[] bids;
    }

    [Serializable]
    public class EraValidators
    {
        public int era_id;
        public ValidatorWeight[] validator_weights;
    }

    [Serializable]
    public class ValidatorWeight
    {
        public string public_key;
        public string weight;
    }

    [Serializable]
    public class BidInfo
    {
        public string public_key;
        public BidData bid;
    }

    [Serializable]
    public class BidData
    {
        public string bonding_purse;
        public string staked_amount;
        public int delegation_rate;
        public bool inactive;
        public Delegator[] delegators;
    }

    [Serializable]
    public class Delegator
    {
        public string public_key;
        public string staked_amount;
        public string bonding_purse;
        public string delegatee;
    }

    /// <summary>
    /// Public model for auction info
    /// </summary>
    public class AuctionInfo
    {
        public string StateRootHash { get; set; }
        public long BlockHeight { get; set; }
        public EraValidatorInfo[] EraValidators { get; set; }
        public ValidatorBid[] Bids { get; set; }
    }

    /// <summary>
    /// Public model for era validator info
    /// </summary>
    public class EraValidatorInfo
    {
        public int EraId { get; set; }
        public ValidatorInfo[] Validators { get; set; }
    }

    /// <summary>
    /// Public model for validator info
    /// </summary>
    public class ValidatorInfo
    {
        public string PublicKey { get; set; }
        public string Weight { get; set; }
        public int EraId { get; set; }
    }

    /// <summary>
    /// Public model for validator bid
    /// </summary>
    public class ValidatorBid
    {
        public string PublicKey { get; set; }
        public string BondingPurse { get; set; }
        public string StakedAmount { get; set; }
        public int DelegationRate { get; set; }
        public bool Inactive { get; set; }
    }
}
