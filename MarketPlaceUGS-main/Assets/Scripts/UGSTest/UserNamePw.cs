using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TMPro;
using Unity.Services.Authentication;
using UnityEngine;
using UnityEngine.UI;
using Unity.Services.Core;


public class UserNamePw : MonoBehaviour
{
    [SerializeField] private TMP_InputField inputID;
    [SerializeField] private TMP_InputField inputPW;
    [SerializeField] private Button loginBtn;
    [SerializeField] private Button siginUpBtn;
    [SerializeField] private TextMeshProUGUI debugLine;
    [SerializeField] private GameObject loginPanel;
    [SerializeField] private GameObject inventoryPanel;

    private PortfolioMarketDemo marketDemo;
    private bool authenticating;
    private Button logoutBtn;

    private void SetAuthenticating(bool value)
    {
        authenticating = value;
        if (loginBtn != null) loginBtn.interactable = !value;
        if (siginUpBtn != null) siginUpBtn.interactable = !value;
        if (logoutBtn != null) logoutBtn.interactable = !value;
    }

    private void Start()
    {
        marketDemo = GetComponent<PortfolioMarketDemo>();

        if (loginBtn != null) loginBtn.onClick.AddListener(OnLogin);
        if (siginUpBtn != null) siginUpBtn.onClick.AddListener(OnSignUp);

        CreateLogoutButton();
        SetLoggedOutUI();
    }

    private void SetLoggedInUI()
    {
        if (loginPanel != null) loginPanel.SetActive(false);
        if (inventoryPanel != null) inventoryPanel.SetActive(true);
        if (inputPW != null) inputPW.text = string.Empty;
        if (logoutBtn != null) logoutBtn.gameObject.SetActive(true);
    }

    private void SetLoggedOutUI()
    {
        if (loginPanel != null) loginPanel.SetActive(true);
        if (inventoryPanel != null) inventoryPanel.SetActive(false);
        if (logoutBtn != null) logoutBtn.gameObject.SetActive(false);
    }

    private void CreateLogoutButton()
    {
        if (loginBtn == null || loginPanel == null) return;
        var welcome = loginPanel.transform.parent.Find("MemberWelcome");
        Transform parent = welcome != null ? welcome : inventoryPanel != null ? inventoryPanel.transform : null;
        if (parent == null) return;

        // Reuse the scene's font, sprite and button styling without Inspector wiring.
        logoutBtn = Instantiate(loginBtn, parent);
        logoutBtn.name = "LogoutButton";
        logoutBtn.onClick = new Button.ButtonClickedEvent();
        logoutBtn.onClick.AddListener(OnLogout);
        var label = logoutBtn.GetComponentInChildren<TextMeshProUGUI>(true);
        if (label != null) label.text = "로그아웃";
        var rect = (RectTransform)logoutBtn.transform;
        rect.anchorMin = new Vector2(.68f, 0);
        rect.anchorMax = new Vector2(.94f, 0);
        rect.pivot = new Vector2(.5f, 0);
        rect.offsetMin = new Vector2(0, 12);
        rect.offsetMax = new Vector2(0, 48);
        var foot = welcome != null ? welcome.Find("WelcomeFoot") as RectTransform : null;
        if (foot != null) foot.anchorMax = new Vector2(.64f, foot.anchorMax.y);
    }

    public void OnLogout()
    {
        if (authenticating) return;
        try
        {
            if (UnityServices.State == ServicesInitializationState.Initialized)
                AuthenticationService.Instance.SignOut(true);
            if (inputID != null) inputID.text = string.Empty;
            if (inputPW != null) inputPW.text = string.Empty;
            SetLoggedOutUI();
            if (marketDemo != null) marketDemo.ResetLoggedOutState();
            SetMessage("로그아웃했습니다. 다시 로그인해 주세요.");
        }
        catch (System.Exception e)
        {
            Debug.LogException(e);
            SetMessage("로그아웃 실패 (Console 확인)");
        }
    }

    private void OnDestroy()
    {
        if (loginBtn != null) loginBtn.onClick.RemoveListener(OnLogin);
        if (siginUpBtn != null) siginUpBtn.onClick.RemoveListener(OnSignUp);
        if (logoutBtn != null) Destroy(logoutBtn.gameObject);
    }

    private void SetMessage(string message)
    {
        if (debugLine != null) debugLine.text = message;
        Debug.Log(message);
    }

    private static bool TryValidatePassword(string password, out string error)
    {
        error = string.Empty;

        if (string.IsNullOrWhiteSpace(password))
        {
            error = "비밀번호가 비어있음";
            return false;
        }

        if (password.Length < 8 || password.Length > 30)
        {
            error = "비밀번호 길이는 8~30";
            return false;
        }

        bool hasUpper = Regex.IsMatch(password, "[A-Z]");
        bool hasLower = Regex.IsMatch(password, "[a-z]");
        bool hasDigit = Regex.IsMatch(password, "[0-9]");
        bool hasSymbol = Regex.IsMatch(password, @"[^A-Za-z0-9]");

        if (!hasUpper || !hasLower || !hasDigit || !hasSymbol)
        {
            error = "대문자 소문자 숫자 특수문자 최소 1개씩 필요 (예: Abcd1234!)";
            return false;
        }

        return true;
    }

    public void OnSignUp()
    {
        _ = SignUpAsync();
    }

    public void OnLogin()
    {
        _ = LoginAsync();
    }

    private async Task SignUpAsync()
    {
        if (authenticating) return;
        SetAuthenticating(true);

        string username = inputID != null ? inputID.text : "";
        string password = inputPW != null ? inputPW.text : "";

        if (!TryValidatePassword(password, out string error))
        {
            SetMessage(error);
            SetAuthenticating(false);
            return;
        }

        try
        {
            await UnityServiceInit.InitializeAsync();
            if (AuthenticationService.Instance.IsSignedIn)
            {
                SetMessage("이미 로그인됨 (회원가입 전에 SignOut 필요)");
                return;
            }

            await AuthenticationService.Instance.SignUpWithUsernamePasswordAsync(username, password);
            SetMessage("회원가입 및 로그인 성공");
            SetLoggedInUI();
            if (marketDemo != null) await marketDemo.RefreshAllAsync();
        }
        catch (AuthenticationException e)
        {
            Debug.LogException(e);
            SetMessage("회원가입 실패 (아이디 중복/정책/네트워크 확인)");
        }
        catch (RequestFailedException e)
        {
            Debug.LogException(e);
            SetMessage("요청 실패 (프로젝트/환경/네트워크 확인)");
        }
        catch (System.Exception e)
        {
            Debug.LogException(e);
            SetMessage("초기화 또는 화면 갱신 실패 (Console 확인)");
        }
        finally
        {
            SetAuthenticating(false);
        }
    }

    private async Task LoginAsync()
    {
        if (authenticating) return;
        SetAuthenticating(true);

        string username = inputID != null ? inputID.text : "";
        string password = inputPW != null ? inputPW.text : "";

        try
        {
            await UnityServiceInit.InitializeAsync();
            if (AuthenticationService.Instance.IsSignedIn)
            {
                SetMessage("이미 로그인된 상태");
                SetLoggedInUI();
                if (marketDemo != null) await marketDemo.RefreshAllAsync();
                return;
            }

            await AuthenticationService.Instance.SignInWithUsernamePasswordAsync(username, password);
            SetMessage("로그인 성공");
            SetLoggedInUI();

            if (marketDemo != null)
            {
                await marketDemo.RefreshAllAsync();
            }
        }
        catch (AuthenticationException e)
        {
            Debug.LogException(e);
            SetMessage("로그인 실패 (아이디/비번 확인)");
        }
        catch (RequestFailedException e)
        {
            Debug.LogException(e);
            SetMessage("요청 실패 (프로젝트/환경/네트워크 확인)");
        }
        catch (System.Exception e)
        {
            Debug.LogException(e);
            SetMessage("초기화 또는 화면 갱신 실패 (Console 확인)");
        }
        finally
        {
            SetAuthenticating(false);
        }
    }
}
