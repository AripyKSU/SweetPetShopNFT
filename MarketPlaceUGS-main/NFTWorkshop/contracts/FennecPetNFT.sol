// SPDX-License-Identifier: MIT
pragma solidity 0.8.30;

import "@openzeppelin/contracts/token/ERC721/extensions/ERC721Enumerable.sol";
import "@openzeppelin/contracts/access/Ownable2Step.sol";
import "@openzeppelin/contracts/utils/ReentrancyGuard.sol";

/// @notice Sepolia pet collection. One public metadata URI is shared by all tokens.
contract FennecPetNFT is ERC721Enumerable, Ownable2Step, ReentrancyGuard {
    struct Coupon { address recipient; uint64 deadline; uint256 tokenId; bool cancelled; }
    mapping(bytes32 => Coupon) public coupons;
    uint256 public immutable maxSupply;
    uint256 public nextTokenId = 1;
    string private metadataURI;
    event CouponRegistered(bytes32 indexed couponHash, address indexed recipient, uint64 deadline);
    event CouponRedeemed(bytes32 indexed couponHash, address indexed recipient, uint256 indexed tokenId);
    event CouponCancelled(bytes32 indexed couponHash);

    constructor(address admin, string memory uri, uint256 limit)
        ERC721("Starlight Fennec Pet NFT", "FENNEC") Ownable(admin) {
        require(limit > 0 && bytes(uri).length > 0, "INVALID_CONFIG");
        maxSupply = limit;
        metadataURI = uri;
    }
    function registerCoupon(bytes32 couponHash, address recipient, uint64 deadline) external onlyOwner {
        require(couponHash != bytes32(0) && recipient != address(0), "INVALID_COUPON");
        require(deadline > block.timestamp, "EXPIRED");
        require(coupons[couponHash].recipient == address(0), "ALREADY_REGISTERED");
        coupons[couponHash] = Coupon(recipient, deadline, 0, false);
        emit CouponRegistered(couponHash, recipient, deadline);
    }
    function cancelCoupon(bytes32 couponHash) external onlyOwner {
        Coupon storage c = coupons[couponHash];
        require(c.recipient != address(0) && c.tokenId == 0 && !c.cancelled, "NOT_ACTIVE");
        c.cancelled = true;
        emit CouponCancelled(couponHash);
    }
    function redeem(bytes32 secret) external nonReentrant returns (uint256 tokenId) {
        bytes32 h = keccak256(abi.encodePacked(secret));
        Coupon storage c = coupons[h];
        require(c.recipient == msg.sender, "WRONG_RECIPIENT");
        require(!c.cancelled && c.tokenId == 0, "COUPON_UNAVAILABLE");
        require(block.timestamp <= c.deadline, "EXPIRED");
        require(nextTokenId <= maxSupply, "SOLD_OUT");
        tokenId = nextTokenId++;
        c.tokenId = tokenId;
        _safeMint(msg.sender, tokenId);
        emit CouponRedeemed(h, msg.sender, tokenId);
    }
    function adminMint(address recipient) external onlyOwner nonReentrant returns (uint256 tokenId) {
        require(nextTokenId <= maxSupply, "SOLD_OUT");
        tokenId = nextTokenId++;
        _safeMint(recipient, tokenId);
    }
    function tokenURI(uint256 tokenId) public view override returns (string memory) {
        _requireOwned(tokenId);
        return metadataURI;
    }
}
