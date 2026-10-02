using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
#if SIMPLE_MARKET_REOWN && !UNITY_WEBGL
using System.Numerics;
using Reown.AppKit.Unity;
using Reown.AppKit.Unity.Model;
#endif

namespace SimpleMarket
{
    // Optional native/Editor backend. Install Reown AppKit 1.7.1, then enable SIMPLE_MARKET_REOWN.
    // Keep Editor's active build target Windows while testing QR (Reown has WebGL-specific code).
    public sealed class ReownWalletBridge : MonoBehaviour
    {
        [Tooltip("Reown Dashboard Project ID, not the UGS Project ID")]
        public string projectId = "";
        public string gameUrl = "https://example.com";
        public string iconUrl = "https://example.com/icon.png";
        [Tooltip("Deployed MythicSwordNFT contract on Sepolia. NFT redemption is zero ETH only.")]
        public string mythicNftContract = "";
        public event Action Changed;
        private bool sending;
        private string lastAccount = "";
        private readonly CancellationTokenSource lifetime = new();
        [Serializable] private class Pending { public string txHash = ""; }
        private static string Key(string owner) => "simple-market-qr-payment:" + owner;
        private static Pending Read(string owner) => PlayerPrefs.HasKey(Key(owner)) ? JsonUtility.FromJson<Pending>(PlayerPrefs.GetString(Key(owner))) : null;
        private static void Save(string owner, string hash)
        {
            PlayerPrefs.SetString(Key(owner), JsonUtility.ToJson(new Pending { txHash = hash })); PlayerPrefs.Save();
        }
        private static void Clear(string owner) { PlayerPrefs.DeleteKey(Key(owner)); PlayerPrefs.Save(); }

        public async Task<WebGlWallet.Result> Call(WebGlWallet.Request request)
        {
            var pending = Read(request.storageKey);
            // Only non-secret receipt references are kept in PlayerPrefs, never balances or keys.
            switch (request.action)
            {
                case "pending": return new() { txHash = pending?.txHash ?? "", status = pending == null ? "EMPTY" : "PENDING" };
                case "clear":
                    if (pending != null && pending.txHash == request.txHash) Clear(request.storageKey);
                    return new() { status = "EMPTY" };
                case "remember":
                    if (sending) throw new Exception("지갑에 열린 요청을 먼저 완료하세요.");
                    if (!Regex.IsMatch(request.txHash ?? "", "^0x[0-9a-fA-F]{64}$")) throw new Exception("거래 해시 형식을 확인하세요.");
                    Save(request.storageKey, request.txHash); return new() { txHash = request.txHash };
                case "forget":
                    if (sending) throw new Exception("지갑에 열린 요청을 먼저 완료하세요.");
                    Clear(request.storageKey); return new() { status = "EMPTY" };
            }
#if SIMPLE_MARKET_REOWN && !UNITY_WEBGL
            await Initialize();
            if (request.action == "connect")
            {
                if (!AppKit.IsAccountConnected && !await TryResumeWalletSession())
                {
                    AppKit.OpenModal();
                    var deadline = DateTime.UtcNow.AddMinutes(3);
                    DateTime? modalClosedAt = null;
                    while (!AppKit.IsAccountConnected)
                    {
                        await Task.Delay(250, lifetime.Token);
                        if (AppKit.IsAccountConnected) break;
                        // Approval can close the modal before the account state is updated.
                        if (AppKit.IsModalOpen) modalClosedAt = null;
                        else
                        {
                            modalClosedAt ??= DateTime.UtcNow;
                            if (DateTime.UtcNow - modalClosedAt.Value >= TimeSpan.FromSeconds(3))
                                throw new Exception("지갑 연결 창이 닫혔습니다. MetaMask에서 승인했다면 지갑 연결을 다시 눌러주세요.");
                        }
                        if (DateTime.UtcNow > deadline) { AppKit.CloseModal(); throw new Exception("QR 연결 대기 시간이 지났습니다. 다시 연결하세요."); }
                    }
                }
                await AppKit.NetworkController.ChangeActiveChainAsync(Sepolia());
            }
            if (!AppKit.IsAccountConnected) throw new Exception("지갑 연결을 먼저 눌러주세요.");
            var account = AppKit.Account;
            if (account.ChainId != "eip155:11155111") throw new Exception("MetaMask에서 Sepolia를 선택하고 다시 연결하세요.");
            if (request.action == "connect" || request.action == "status")
            {
                var balance = await AppKit.Evm.GetBalanceAsync(account.Address);
                lastAccount = account.AccountId;
                return new() { address = account.Address, chainId = "0xaa36a7", balanceEth = ((decimal)balance / 1000000000000000000m).ToString("0.######", CultureInfo.InvariantCulture) };
            }
            if (request.action == "nftSign")
            {
                if (sending) throw new Exception("지갑 요청을 먼저 완료하세요.");
                if (!string.Equals(account.Address, request.from, StringComparison.OrdinalIgnoreCase) ||
                    !(request.data ?? "").StartsWith("Mythic Sword NFT - Link wallet\n", StringComparison.Ordinal) || request.data.Length > 2000)
                    throw new Exception("NFT 지갑 연결 서명 요청을 확인하세요.");
                sending = true;
                try
                {
                    var hex = "0x" + BitConverter.ToString(System.Text.Encoding.UTF8.GetBytes(request.data)).Replace("-", "");
                    var signature = await AppKit.Instance.SignClient.RequestAsync<string[], string>(
                        "personal_sign", new[] { hex, account.Address }, chainId: "eip155:11155111", ct: lifetime.Token);
                    return new() { signature = signature };
                }
                finally { sending = false; }
            }
            if (request.action == "nftRedeem")
            {
                if (sending) throw new Exception("지갑 요청을 먼저 완료하세요.");
                if (!string.Equals(account.Address, request.from, StringComparison.OrdinalIgnoreCase) ||
                    !Regex.IsMatch(mythicNftContract ?? "", "^0x[0-9a-fA-F]{40}$") ||
                    !string.Equals(request.to, mythicNftContract, StringComparison.OrdinalIgnoreCase) ||
                    request.value != "0x0" || !Regex.IsMatch(request.data ?? "", "^0xeda1122c[0-9a-fA-F]{64}$"))
                    throw new Exception("NFT 계약 또는 쿠폰 요청을 확인하세요.");
                sending = true;
                try
                {
                    var nftTx = new Dictionary<string, object> { { "from", account.Address }, { "to", mythicNftContract },
                        { "value", "0x0" }, { "data", request.data }, { "chainId", "0xaa36a7" } };
                    var nftHash = await AppKit.Instance.SignClient.RequestAsync<Dictionary<string, object>[], string>(
                        "eth_sendTransaction", new[] { nftTx }, chainId: "eip155:11155111", ct: lifetime.Token);
                    if (!Regex.IsMatch(nftHash ?? "", "^0x[0-9a-fA-F]{64}$")) throw new Exception("거래 해시를 MetaMask에서 확인하세요.");
                    return new() { txHash = nftHash };
                }
                finally { sending = false; }
            }
            if (request.action != "send") throw new Exception("지원하지 않는 지갑 요청입니다.");
            if (sending || pending != null) throw new Exception("이전 결제를 먼저 확인하세요.");
            if (!string.Equals(account.Address, request.from, StringComparison.OrdinalIgnoreCase)) throw new Exception("지갑 계정이 바뀌었습니다. 다시 연결하세요.");
            if (!Regex.IsMatch(request.to ?? "", "^0x[0-9a-fA-F]{40}$") || Regex.IsMatch(request.to, "^0x0{40}$") || string.Equals(account.Address, request.to, StringComparison.OrdinalIgnoreCase)) throw new Exception("수신 지갑과 다른 구매자 지갑을 사용하세요.");
            if (request.value != "0x5af3107a4000" || !Regex.IsMatch(request.data ?? "", "^0x[0-9a-fA-F]+$")) throw new Exception("결제 정보가 올바르지 않습니다.");
            Save(request.storageKey, "");
            sending = true;
            try
            {
                // Includes the server's account-binding memo. Never send a plain ETH transfer here.
                // Omit optional fields: the SDK's TransactionInput serializes null gas/fee
                // fields which MetaMask rejects before showing its approval screen.
                var transaction = new Dictionary<string, object>
                {
                    { "from", account.Address }, { "to", request.to },
                    { "value", request.value }, { "data", request.data },
                    { "chainId", "0xaa36a7" }
                };
                // Call the connected WalletConnect session directly. Evm.RpcRequestAsync
                // passes through Nethereum's interceptor, which casts parameters to
                // TransactionInput and cannot accept this null-free dictionary.
                var hash = await AppKit.Instance.SignClient.RequestAsync<Dictionary<string, object>[], string>(
                    "eth_sendTransaction", new[] { transaction }, chainId: "eip155:11155111", ct: lifetime.Token);
                if (!Regex.IsMatch(hash ?? "", "^0x[0-9a-fA-F]{64}$")) throw new Exception("MetaMask 활동에서 거래 해시를 확인하세요.");
                Save(request.storageKey, hash);
                return new() { txHash = hash };
            }
            catch (Reown.Core.Common.Model.Errors.ReownNetworkException e) when
                (e.Message.Contains("External transactions to internal accounts cannot include data"))
            {
                // MetaMask rejected this request before approval/broadcast.
                Clear(request.storageKey);
                throw new Exception("수신 주소가 구매용 MetaMask에 등록된 계정입니다. Cloud Save config의 receiverAddress를 별도 지갑 주소로 변경하세요.", e);
            }
            finally { sending = false; } // Unknown failures retain the marker. Never automatically re-send.
#else
            await Task.Yield();
            throw new Exception("Editor QR 준비: Reown AppKit 1.7.1 설치 → Prefab 추가 → Project ID 입력 → SIMPLE_MARKET_REOWN 활성화. Editor 플랫폼은 Windows로 두세요.");
#endif
        }
#if SIMPLE_MARKET_REOWN && !UNITY_WEBGL
        private async Task<bool> TryResumeWalletSession()
        {
            try { return await AppKit.ConnectorController.TryResumeSessionAsync(); }
            catch (NullReferenceException)
            {
                // Reown LoadDefaultsAsync dereferences Session after a remote disconnect.
                // Remove only its default-session record; payment receipts stay intact.
                var client = AppKit.Instance.SignClient;
                await client.CoreClient.Storage.RemoveItem(client.AddressProvider.Context + "-default-session");
                await client.AddressProvider.LoadDefaultsAsync();
                Debug.LogWarning("[ReownWalletBridge] 끊어진 지갑 세션을 정리했습니다. QR로 다시 연결하세요.");
                return false;
            }
        }
        private static Chain Sepolia() => new("eip155", "11155111", "Ethereum Sepolia",
            new Currency("Sepolia Ether", "ETH", 18), new BlockExplorer("Etherscan", "https://sepolia.etherscan.io"),
            "https://ethereum-sepolia-rpc.publicnode.com", true, ChainConstants.Chains.Ethereum.ImageUrl);
        private async Task Initialize()
        {
            if (AppKit.IsInitialized) return;
            if (!Regex.IsMatch(projectId ?? "", "^[a-fA-F0-9]{32}$")) throw new Exception("Reown Project ID를 입력하세요. UGS Project ID가 아닙니다.");
            await AppKit.InitializeAsync(new AppKitConfig(projectId, new Metadata("RPG Market", "Sepolia gold purchase", gameUrl, iconUrl))
            {
                supportedChains = new[] { Sepolia() },
                includedWalletIds = new[] { "c57ca95b47569778a828d19178114f4db188b89b763c899ba0be274e97267d96" },
                enableAnalytics = false, enableEmail = false, enableOnramp = false,
                socials = Array.Empty<SocialLogin>()
            });
        }
#endif
        private void Update()
        {
#if SIMPLE_MARKET_REOWN && !UNITY_WEBGL
            if (!AppKit.IsInitialized) return;
            string account = AppKit.IsAccountConnected ? AppKit.Account.AccountId : "";
            if (account != lastAccount) { lastAccount = account; Changed?.Invoke(); }
#endif
        }
        private void OnDestroy() { lifetime.Cancel(); lifetime.Dispose(); }
    }
}
