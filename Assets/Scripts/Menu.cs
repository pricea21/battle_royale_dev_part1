using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using Photon.Realtime;

public class Menu : MonoBehaviourPunCallbacks, ILobbyCallbacks
{
	[Header("Screens")]
	public GameObject mainScreen;
	public GameObject createRoomScreen;
	public GameObject lobbyScreen;
	public GameObject lobbyBrowserScreen;

	[Header("Main SCreen")]
	public Button createRoomButton;
	public Button findRoomButton;

	[Header("Lobby")]
	public TextMeshProUGUI playerListText;
	public TextMeshProUGUI roomInfoText;
	public Button startGameButton;

	[Header("Lobby Browser")]
	public RectTransform roomListContainer;
	public GameObject roomButtonPrefab;

	private List<GameObject> roomButtons = new List<GameObject>();
	private List<RoomInfo> roomList = new List<RoomInfo>();

	void Start()
	{
		//disable the menu buttons at the start 
		createRoomButton.interactable = false;
		findRoomButton.interactable = false;

		//enable the cursor since we hide it when in game 
		Cursor.lockState = CursorLockMode.None;

		//are we in game?
		if(PhotonNetwork.InRoom)
		{
			//go to the lobby

			//make the room visible again 
			PhotonNetwork.CurrentRoom.IsVisible = true;
			PhotonNetwork.CurrentRoom.IsOpen = true;
		}
	}

	//changes the currently visible screen
	void SetScreen (GameObject screen)
	{
		//disable all other screens 
		mainScreen.SetActive(false);
		createRoomScreen.SetActive(false);
		lobbyScreen.SetActive(false);
		lobbyBrowserScreen.SetActive(false);

		//activate all requested screens
		screen.SetActive(true);

		if(screen == lobbyBrowserScreen)
			UpdateLobbyBrowserUI();
	}

	//gets called when the back button is pressed
	public void OnBackButton()
	{
		SetScreen(mainScreen);
	}

	//Main Screen

	public void OnPlayerNameValueChanged(TMP_InputField playerNameInput)
	{
		PhotonNetwork.NickName = playerNameInput.text;
	}

	public override void OnConnectedToMaster()
	{
		//enable the menu buttons when connected to server
		createRoomButton.interactable = true;
		findRoomButton.interactable = true;
	}

	//called when create room button is clicked
	public void OnCreateRoomButton()
	{
		SetScreen(createRoomScreen);
	}

	//called when find room button is clicked
	public void OnFindRoomButton()
	{
		SetScreen(lobbyBrowserScreen);
	}

	//Create Room Screen
	public void OnCreateButton(TMP_InputField roomNameInput)
	{
		NetworkManager.instance.CreateRoom(roomNameInput.text);
	}

	//Lobby Screen

	public override void OnJoinedRoom()
	{
		SetScreen(lobbyScreen);
		photonView.RPC("UpdateLobbyUI",RpcTarget.All);
	}

	public override void OnPlayerLeftRoom(Player otherPLayer)
	{
		UpdateLobbyUI();
	}

	[PunRPC]
	void UpdateLobbyUI()
	{
		//enable or diable the start button depending on if were the host
		startGameButton.interactable = PhotonNetwork.IsMasterClient;

		//display all the players
		playerListText.text = "";

		foreach(Player player in PhotonNetwork.PlayerList)
			playerListText.text += player.NickName + "\n";

		//set room info text
		roomInfoText.text = "<b>Room Name </b>\n" + PhotonNetwork.CurrentRoom.Name;
	}

	public void OnStartGameButton()
	{
		//hide the room
		PhotonNetwork.CurrentRoom.IsOpen = false;
		PhotonNetwork.CurrentRoom.IsVisible = false;

		//tell everyone to load into the game scene
		NetworkManager.instance.photonView.RPC("ChangeScene", RpcTarget.All, "Game");
	}

	public void OnLeaveLobbyButton()
	{
		PhotonNetwork.LeaveRoom();
		SetScreen(mainScreen);
	}

	//Lobby Browser Screen

	GameObject CreateRoomButton()
	{
		GameObject buttonObj = Instantiate(roomButtonPrefab, roomListContainer.transform);
		roomButtons.Add(buttonObj);

		return buttonObj;
	}

	void UpdateLobbyBrowserUI()
	{
		//disable all room buttons
		foreach(GameObject button in roomButtons)
			button.SetActive(false);

		//display all current rooms in the master server
		for(int x =0; x < roomList.Count; ++x)
		{
			//get or create the button object
			GameObject button = x >= roomButtons.Count ? CreateRoomButton() : roomButtons[x];

			button.SetActive(true);

			//set the room name and player count text
			button.transform.Find("RoomNameText").GetComponent<TextMeshProUGUI>().text = roomList[x].Name;
			button.transform.Find("PlayerCountText").GetComponent<TextMeshProUGUI>().text = roomList[x].PlayerCount + " / " + roomList[x].MaxPlayers;

			//set the button Onclick event
			Button buttonComp = button.GetComponent<Button>();

			string roomName = roomList[x].Name;

			buttonComp.onClick.RemoveAllListeners();
			buttonComp.onClick.AddListener(() => { OnJoinedRoomButton(roomName); });
		}
	}

	public void OnJoinedRoomButton(string roomName)
	{
		NetworkManager.instance.JoinRoom(roomName);
	}

	public void OnRefreshButton()
	{
		UpdateLobbyBrowserUI();
	}

	public override void OnRoomListUpdate(List <RoomInfo> allRooms)
	{
		roomList = allRooms;
	}
}
