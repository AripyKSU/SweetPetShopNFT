using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TMPro;
using Unity.Services.Authentication;
using Unity.Services.CloudCode;
using Unity.Services.Core;
using UnityEngine;
using UnityEngine.UI;

namespace SimpleMarket
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(WebGlWallet), typeof(ReownWalletBridge))]
    public sealed class SceneWalletPanel : MonoBehaviour
    {
        [Header("Scene 1 - existing UI")]
        public Button walletButton;
        public Button paymentCheckButton;
        public Button buy10000GoldButton;
        public Button buyLegendarySwordButton;
        public Button swordPaymentCheckButton;
        public TMP_Text walletAddressText;
        public TMP_Text sepoliaEthText;
        public TMP_Text paymentStatusText;
        [Header("Existing marketplace")]
        public PortfolioMarketDemo marketDemo;
        [Tooltip("Must match the environment used by your existing UGS initialization. Local recovery key only.")]
        public string environmentName = "production";
        [Header("Optional recovery UI")]
        public TMP_InputField transactionHashInput;
        public Button forgetPaymentButton;

        private WebGlWallet browser;
        private ReownWalletBridge qr;
        private string observedPlayer = "", connectedAddress = "";
        private bool busy, connected, pending, swordPending;
        private readonly CancellationTokenSource lifetime = new();
        [Serializable] private class Quote { public string chainId, to, value, data, priceEth, itemId; public int goldAmount, confirmations, quantity; }
        [Serializable] private class Receipt { public string status, txHash, pendingReason, itemId, instanceId; public int goldAmount, quantity; }

        private string Player => UnityServices.State == ServicesInitializationState.Initialized && AuthenticationService.Instance.IsSignedIn ? AuthenticationService.Instance.PlayerId : "";
        private string OwnerKey(string player, bool sword = false) => Application.cloudProjectId + ":" + environmentName + ":" + player + (sword ? ":legendary-sword-v1" : "");
        private void Awake()
        {
            browser = GetComponent<WebGlWallet>(); qr = GetComponent<ReownWalletBridge>();
            browser.Changed += WalletChanged; qr.Changed += WalletChanged;
            if (walletButton) walletButton.onClick.AddListener(ConnectWallet);
            if (paymentCheckButton) paymentCheckButton.onClick.AddListener(CheckPayment);
            if (buy10000GoldButton) buy10000GoldButton.onClick.AddListener(BuyGold);
            if (buyLegendarySwordButton) buyLegendarySwordButton.onClick.AddListener(BuyLegendarySword);
            if (swordPaymentCheckButton) swordPaymentCheckButton.onClick.AddListener(CheckSwordPayment);
            if (forgetPaymentButton) forgetPaymentButton.onClick.AddListener(ForgetPayment);
            WalletChanged(); Message("게임 로그인 후 지갑을 연결하세요.");
        }
        private void Update()
        {
            string player = Player;
            if (player == observedPlayer) return;
            observedPlayer = player; pending = false; swordPending = false; WalletChanged();
            if (transactionHashInput) transactionHashInput.text = "";
            Message(player == "" ? "게임 로그인 후 지갑을 연결하세요." : "지갑 연결을 누르세요. 이전 결제는 결제 확인으로 복구할 수 있습니다.");
            Buttons();
        }
        private void WalletChanged()
        {
            connected = false; connectedAddress = "";
            if (walletAddressText) walletAddressText.text = "지갑: 미연결 (계정 변경 시 다시 연결)";
            if (sepoliaEthText) sepoliaEthText.text = "Sepolia ETH: -";
            Buttons();
        }
        private void Buttons()
        {
            bool allowed = !busy && Player != "";
            if (walletButton) walletButton.interactable = allowed;
            if (paymentCheckButton) paymentCheckButton.interactable = allowed;
            if (buy10000GoldButton) buy10000GoldButton.interactable = allowed && connected && !pending;
            if (buyLegendarySwordButton) buyLegendarySwordButton.interactable = allowed && connected && !swordPending;
            if (swordPaymentCheckButton) swordPaymentCheckButton.interactable = allowed;
            if (forgetPaymentButton) forgetPaymentButton.interactable = allowed;
        }
        private void Message(string text) { if (paymentStatusText) paymentStatusText.text = text; }
        private void CheckOwner(string player)
        {
            lifetime.Token.ThrowIfCancellationRequested();
            if (Player != player || player == "") throw new Exception("게임 계정이 변경되었습니다. 원래 계정으로 로그인해서 결제 확인을 누르세요.");
        }
        private async Task Run(Func<string, Task> action)
        {
            if (busy) return;
            string player = Player;
            if (player == "") { Message("게임 로그인이 필요합니다."); return; }
            busy = true; Buttons();
            try { await action(player); }
            catch (OperationCanceledException) { }
            catch (Exception e)
            {
                if (!lifetime.IsCancellationRequested && Player == player)
                {
                    string error = e is CloudCodeException ? e.ToString() : e.Message;
                    Message(FriendlyError(error));
                    Debug.LogWarning("[SceneWalletPanel] " + e);
                }
            }
            finally { busy = false; if (this) Buttons(); }
        }
        private static string FriendlyError(string error)
        {
            if (error.Contains("RECEIVER_MUST_BE_EOA")) return "상점 수신 지갑에 스마트 계정/컨트랙트 코드가 있습니다. Cloud Save simple_market/config.receiverAddress를 확인하세요 (README 12.0절).";
            if (error.Contains("REVIEW_REQUIRED")) return "지급 결과를 관리자가 확인해야 합니다. 거래 해시를 보관하고 재결제하지 마세요.";
            if (error.Contains("SETUP_REQUIRED")) return "Cloud Save classroom_market의 Private state와 Cloud Code 배포를 확인하세요.";
            if (error.Contains("WRONG_PLAYER")) return "결제한 게임 계정과 상품(골드/특별 펫)에 맞는 확인 버튼을 사용하세요.";
            if (error.Contains("RPC_UNAVAILABLE")) return "블록체인 조회가 지연됩니다. 잠시 후 결제 확인을 누르세요.";
            if (error.Contains("INVALID_")) return "거래 정보 또는 서버 설정이 일치하지 않습니다. 수신 주소·금액·거래 해시를 확인하세요.";
            if (error.Contains("TRANSACTION_FAILED")) return "실패한 블록체인 거래입니다. 골드가 지급되지 않았습니다.";
            if (error.Contains("ScriptError") || error.Contains("NotFound")) return "Gold_GetQuote / Gold_Claim 배포와 파라미터를 확인하세요.";
            return error;
        }
        private Task<WebGlWallet.Result> Wallet(WebGlWallet.Request request)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return browser.Call(request);
#else
            return qr.Call(request);
#endif
        }
        private async Task<WebGlWallet.Result> Pending(string player, bool sword = false)
        {
            var result = await Wallet(new() { action = "pending", storageKey = OwnerKey(player, sword) });
            CheckOwner(player);
            if (sword) swordPending = result.status == "PENDING"; else pending = result.status == "PENDING";
            return result;
        }
        public void ConnectWallet() => _ = Run(async player =>
        {
            Message("연결 창에서 MetaMask를 선택하고 모바일 앱으로 QR을 스캔하세요. WebGL에서는 확장 프로그램을 사용합니다.");
            var result = await Wallet(new() { action = "connect", storageKey = OwnerKey(player) });
            CheckOwner(player);
            connectedAddress = result.address;
            connected = !string.IsNullOrEmpty(result.address) && string.Equals(result.chainId, "0xaa36a7", StringComparison.OrdinalIgnoreCase);
            if (walletAddressText) walletAddressText.text = "지갑: " + result.address;
            if (sepoliaEthText) sepoliaEthText.text = "Sepolia ETH: " + result.balanceEth;
            await Pending(player);
            await Pending(player, true);
            Message(pending || swordPending ? "이전 결제가 있습니다. 해당 상품의 결제 확인을 누르세요." : "연결 완료. 골드 또는 특별 펫 입양을 선택하세요.");
        });
        public void BuyGold() => BuyProduct(false);
        public void BuyLegendarySword() => BuyProduct(true);
        private void BuyProduct(bool sword) => _ = Run(async player =>
        {
            var stored = await Pending(player, sword);
            if (stored.status == "PENDING") throw new Exception("이전 결제를 먼저 확인하세요.");
            var quote = await CloudCodeService.Instance.CallEndpointAsync<Quote>(sword ? "Sword_GetQuote" : "Gold_GetQuote", new Dictionary<string, object>());
            CheckOwner(player);
            if (quote.chainId != "0xaa36a7" || quote.value != "0x5af3107a4000" || quote.priceEth != "0.0001" ||
                (sword ? quote.itemId != "LEGENDARY_SWORD" || quote.quantity != 1 : quote.goldAmount != 10000)) throw new Exception("INVALID_QUOTE");
            string from = connectedAddress;
            if (!connected || string.IsNullOrEmpty(from)) throw new Exception("지갑을 다시 연결하세요.");
            Message((sword ? "특별 펫 골든 리트리버 1마리: " : "10,000골드: ") + "MetaMask에서 0.0001 Sepolia ETH + 가스비를 승인하세요.");
            try
            {
                var sent = await Wallet(new() { action = "send", storageKey = OwnerKey(player, sword), from = from, to = quote.to, value = quote.value, data = quote.data });
                CheckOwner(player);
                if (sword) swordPending = true; else pending = true;
                Message("전송 완료. 블록체인 승인 및 UGS 지급 확인 중...");
                // Release the UI after one check; pending receipts remain saved for manual retry.
                await Claim(player, sent.txHash, sword);
            }
            finally { if (this && Player == player) await Pending(player, sword); }
        });
        public void CheckPayment() => CheckProductPayment(false);
        public void CheckSwordPayment() => CheckProductPayment(true);
        private void CheckProductPayment(bool sword) => _ = Run(async player =>
        {
            string manualHash = transactionHashInput ? transactionHashInput.text.Trim() : "";
            var stored = await Pending(player, sword);
            string hash = string.IsNullOrEmpty(manualHash) ? stored.txHash : manualHash;
            if (string.IsNullOrEmpty(hash))
            {
                Message(stored.status == "PENDING" ? "송금 결과 미확인. MetaMask 활동을 확인하세요. 전송이 없으면 해당 상품의 Recovery를 사용하세요." : "이 상품에 확인할 결제가 없습니다.");
                return;
            }
            CheckOwner(player);
            // A manually entered hash is not persisted until the server accepts it.
            await Claim(player, hash, sword);
        });
        private async Task<bool> Claim(string player, string hash, bool sword = false)
        {
            CheckOwner(player);
            Receipt receipt;
            try { receipt = await CloudCodeService.Instance.CallEndpointAsync<Receipt>(sword ? "Sword_Claim" : "Gold_Claim", new Dictionary<string, object> { { "tx_hash", hash } }); }
            catch (CloudCodeException e) when (e.ToString().Contains("TRANSACTION_FAILED"))
            {
                await Wallet(new() { action = "clear", storageKey = OwnerKey(player, sword), txHash = hash });
                if (Player == player)
                {
                    if (sword) swordPending = false; else pending = false;
                    if (transactionHashInput) transactionHashInput.text = "";
                }
                throw new Exception("TRANSACTION_FAILED");
            }
            CheckOwner(player);
            if (receipt.status == "PENDING")
            {
                string reason = string.IsNullOrEmpty(receipt.pendingReason) ? "OLD_SERVER: 해당 Claim 진단 버전을 Publish하세요." : receipt.pendingReason;
                Message("입금 확인 대기: " + reason + "\n잠시 후 " + (sword ? "특별 펫 결제 확인" : "골드 결제 확인") + "을 누르세요. 재결제는 필요 없습니다.");
                Debug.LogWarning("[" + (sword ? "Sword" : "Gold") + "ClaimDiagnostic] tx=" + hash + " reason=" + reason);
                return false;
            }
            if (receipt.status != "GRANTED")
            {
                Debug.LogWarning("[" + (sword ? "Sword" : "Gold") + "ClaimDiagnostic] tx=" + hash + " reason=" + (receipt.pendingReason ?? "REVIEW_REQUIRED"));
                throw new Exception("REVIEW_REQUIRED");
            }
            if (sword && (receipt.itemId != "LEGENDARY_SWORD" || receipt.quantity != 1)) throw new Exception("INVALID_RECEIPT");
            await Wallet(new() { action = "clear", storageKey = OwnerKey(player, sword), txHash = hash });
            CheckOwner(player);
            await Pending(player, sword);
            if (transactionHashInput) transactionHashInput.text = "";
            if (marketDemo != null)
            {
                if (sword) await marketDemo.RefreshInventoryAsync(); else await marketDemo.RefreshCoinsAsync();
            }
            CheckOwner(player);
            Message(sword ? "결제 확인 완료. 특별 펫 골든 리트리버가 보관소에 추가되었습니다." : "결제 확인 완료. UGS에 10,000골드가 지급되었습니다.");
            return true;
        }
        public void ForgetPayment() => _ = Run(async player =>
        {
            await Wallet(new() { action = "forget", storageKey = OwnerKey(player) });
            CheckOwner(player); pending = false;
            Message("로컬 확인 기록만 해제했습니다. 환불이나 거래취소가 아닙니다. 재결제 전 MetaMask 활동을 확인하세요.");
        });
        public void ForgetSwordPayment() => _ = Run(async player =>
        {
            await Wallet(new() { action = "forget", storageKey = OwnerKey(player, true) });
            CheckOwner(player); swordPending = false;
            Message("특별 펫 결제의 로컬 기록만 해제했습니다. 재결제 전 MetaMask 활동을 확인하세요.");
        });
#if UNITY_EDITOR
        [ContextMenu("Recovery - Clear unsubmitted SWORD payment")]
        private void RecoverUnsubmittedSwordPayment()
        {
            if (!Application.isPlaying) { Debug.LogWarning("Play 모드에서 로그인 후 실행하세요."); return; }
            if (UnityEditor.EditorUtility.DisplayDialog("특별 펫 결제 기록 해제", "MetaMask에 특별 펫 결제 송금과 대기 요청이 없음을 확인했나요? 전송됐다면 취소하고 특별 펫 결제 확인을 사용하세요.", "전송 없음 - 해제", "취소")) ForgetSwordPayment();
        }
        [ContextMenu("Recovery - Clear unsubmitted payment")]
        private void RecoverUnsubmittedPayment()
        {
            if (!Application.isPlaying) { Debug.LogWarning("Play 모드에서 게임 로그인 후 실행하세요."); return; }
            if (UnityEditor.EditorUtility.DisplayDialog("결제 대기 기록 해제",
                "MetaMask 활동에 이번 송금이 없고, 대기 중인 승인 요청도 없음을 확인했나요?\n전송된 거래가 있다면 취소를 누르고 거래 해시로 결제 확인하세요.\n이 작업은 로컬 기록만 지우며 환불하거나 송금하지 않습니다.",
                "송금·승인 요청 없음 — 기록 해제", "취소")) ForgetPayment();
        }
#endif
        private void OnDestroy()
        {
            lifetime.Cancel();
            if (walletButton) walletButton.onClick.RemoveListener(ConnectWallet);
            if (paymentCheckButton) paymentCheckButton.onClick.RemoveListener(CheckPayment);
            if (buy10000GoldButton) buy10000GoldButton.onClick.RemoveListener(BuyGold);
            if (buyLegendarySwordButton) buyLegendarySwordButton.onClick.RemoveListener(BuyLegendarySword);
            if (swordPaymentCheckButton) swordPaymentCheckButton.onClick.RemoveListener(CheckSwordPayment);
            if (forgetPaymentButton) forgetPaymentButton.onClick.RemoveListener(ForgetPayment);
            if (browser) browser.Changed -= WalletChanged;
            if (qr) qr.Changed -= WalletChanged;
            lifetime.Dispose();
        }
    }
}
