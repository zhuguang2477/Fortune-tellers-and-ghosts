using UnityEngine;
using UnityEngine.UI;
using FishNet;
using FishNet.Transporting;

public class MyNetworkUI : MonoBehaviour
{
    public GameObject panel;
    public InputField addressInput;
    public InputField portInput;
    public Button hostButton;
    public Button clientButton;
    public Button serverOnlyButton;
    public Button stopButton;
    public Text statusText;

    private string defaultAddress = "localhost";
    private string defaultPort = "7777";

    void Start()
    {
        if (panel != null)
            panel.SetActive(false);

        addressInput.text = defaultAddress;
        portInput.text = defaultPort;

        hostButton.onClick.AddListener(StartHost);
        clientButton.onClick.AddListener(StartClient);
        serverOnlyButton.onClick.AddListener(StartServerOnly);
        stopButton.onClick.AddListener(StopAll);

        stopButton.gameObject.SetActive(false);
    }

    void Update()
    {
        UpdateUIState();
    }

    void UpdateUIState()
    {
        LocalConnectionState clientState = InstanceFinder.TransportManager.Transport.GetConnectionState(false);
        LocalConnectionState serverState = InstanceFinder.TransportManager.Transport.GetConnectionState(true);

        bool isConnected = (clientState != LocalConnectionState.Stopped) || (serverState != LocalConnectionState.Stopped);

        hostButton.interactable = !isConnected;
        clientButton.interactable = !isConnected;
        serverOnlyButton.interactable = !isConnected;
        stopButton.gameObject.SetActive(isConnected);

        if (clientState == LocalConnectionState.Starting)
            statusText.text = "Connecting...";
        else if (clientState == LocalConnectionState.Started)
            statusText.text = $"Client connected to {addressInput.text}";
        else if (serverState == LocalConnectionState.Starting)
            statusText.text = "Starting server...";
        else if (serverState == LocalConnectionState.Started)
            statusText.text = "Server running";
        else
            statusText.text = "Disconnected";
    }

    void StartHost()
    {
        #if !UNITY_WEBGL
        InstanceFinder.NetworkManager.TransportManager.Transport.SetClientAddress("localhost");
        InstanceFinder.NetworkManager.TransportManager.Transport.SetPort(ushort.Parse(portInput.text));
        InstanceFinder.ServerManager.StartConnection();
        InstanceFinder.ClientManager.StartConnection();
        #endif
    }

    void StartClient()
    {
        InstanceFinder.NetworkManager.TransportManager.Transport.SetClientAddress(addressInput.text);
        InstanceFinder.NetworkManager.TransportManager.Transport.SetPort(ushort.Parse(portInput.text));
        InstanceFinder.ClientManager.StartConnection();
    }

    void StartServerOnly()
    {
        #if !UNITY_WEBGL
        InstanceFinder.NetworkManager.TransportManager.Transport.SetPort(ushort.Parse(portInput.text));
        InstanceFinder.ServerManager.StartConnection();
        #endif
    }

    void StopAll()
    {
        InstanceFinder.TransportManager.Transport.StopConnection(true);
    }

    public void ShowPanel()
    {
        if (panel != null)
            panel.SetActive(true);
    }

    public void HidePanel()
    {
        if (panel != null)
            panel.SetActive(false);
    }
}