using ExtensionsMethods;
using LosSantosRED.lsr;
using LosSantosRED.lsr.Helper;
using LosSantosRED.lsr.Interface;
using Rage;
using Rage.Native;
using RAGENativeUI;
using RAGENativeUI.Elements;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Timers;
using System.Windows.Forms;
using static DispatchScannerFiles;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TrayNotify;

public class CellPhone
{
    private bool IsDisposed = false;
    private ICellPhoneable Player;
    private int ContactIndex = 40;
    private int BurnerContactIndex = 0;
    private int TextIndex = 0;
    private MenuPool MenuPool;
    private IJurisdictions Jurisdictions;
    private List<PhoneContact> AddedContacts = new List<PhoneContact>();
    private IInventoryable Inventory;
    private IAgencies Agencies;
    private IContacts Contacts;
    private ISettingsProvideable Settings;
    private ITimeReportable Time;
    private IGangs Gangs;
    private List<PhoneText> AddedTexts = new List<PhoneText>();
    private IPlacesOfInterest PlacesOfInterest;
    private IZones Zones;
    private IStreets Streets;
    private List<ScheduledContact> ScheduledContacts = new List<ScheduledContact>();
    private List<ScheduledText> ScheduledTexts = new List<ScheduledText>();
    private IGangTerritories GangTerritories;
    private int TextSound;
    private List<PhoneResponse> PhoneResponses = new List<PhoneResponse>();
    private GunDealerInteraction GunDealerInteraction;
    private GangInteraction GangInteraction;
    private IContactInteractable ContactInteractable;
    private CorruptCopInteraction CorruptCopInteraction;
    private EmergencyServicesInteraction EmergencyServicesInteraction;
    private bool isRunningForcedMobileTask;
    private IEntityProvideable World;
    private ICrimes Crimes;
    private IModItems ModItems;
    private uint GameTimeLastCheckedScheduledItems;
    private uint GameTimeBetweenCheckScheduledItems = 1000;
    private NAudioPlayer phoneAudioPlayer;
    private IWeapons Weapons;
    private INameProvideable Names;
    private IShopMenus ShopMenus;
    private System.Timers.Timer DrugRequestTimer;
    private Random DrugRequestRandom = new Random();
    private bool IsDisposingDrugRequestTimer = false;
    private readonly List<string> DrugRequestCustomerNames = new List<string>() {
    "Jamie",
    "Alex",
    "Jordan",
    "Taylor",
    "Casey",
    "Riley",
    "Morgan",
    "Quinn",
    "Pat",
    "Sam"
};
    private readonly List<string> DrugRequestMessages = new List<string>() {
    "Hey, you holding? I need something to get through the day.",
    "Need a pickup, got anything good?",
    "My dealer's gone dry. Heard you might have what I need.",
    "In town for the weekend, looking for a good time if you know what I mean.",
    "Friend gave me your number. Said you could help with my 'medical condition'.",
    "I'll pay extra if you can deliver within the hour.",
    "Desperate times, need something strong. Name your price.",
    "Regular customer needs a re-up, you know what I like.",
    "Party tonight, need supplies. Can you help?",
    "Been a while, got anything new in stock?"
};

    private Dictionary<string, Blip> ActiveDrugDealBlips = new Dictionary<string, Blip>();

    private class DrugBuyerPed : PedExt
    {
        public DrugBuyerPed(Ped ped, string name)
            : base(ped, null, null, null, name, "DrugBuyer", null)
        {
            // This will work as long as the PedExt base constructor doesn't immediately use the null parameters
            // The alternative is to directly use the Ped object without PedExt
        }

        // Override any methods that might cause errors due to null parameters
    }

    private ICellphones Cellphones;
    public CellphoneData CurrentCellphoneData { get; private set; }

    public BurnerPhone BurnerPhone { get; private set; }

    public string RingTone => !string.IsNullOrEmpty(CustomRingtone) ? CustomRingtone : Settings.SettingsManager.CellphoneSettings.DefaultCustomRingtoneNameNew;
    public string TextTone => !string.IsNullOrEmpty(CustomTextTone) ? CustomTextTone : Settings.SettingsManager.CellphoneSettings.DefaultCustomTexttoneNameNew;
    public int Theme => CustomTheme != -1 ? CustomTheme : Settings.SettingsManager.CellphoneSettings.DefaultBurnerCellThemeID;
    public int Background => CustomBackground != -1 ? CustomBackground : Settings.SettingsManager.CellphoneSettings.DefaultBurnerCellBackgroundID;
    public float Volume => CustomVolume != -1.0f ? CustomVolume : Settings.SettingsManager.CellphoneSettings.DefaultCustomToneVolume;
    public bool SleepMode { get; set; } = false;
    public int PhoneType => CustomPhoneType != -1 ? CustomPhoneType : CurrentCellphoneData != null ? CurrentCellphoneData.PhoneType : Settings.SettingsManager.CellphoneSettings.BurnerCellPhoneTypeID;
    public string PhoneOS => CustomPhoneOS != "" ? CustomPhoneOS : CurrentCellphoneData != null ? CurrentCellphoneData.PhoneOS : Settings.SettingsManager.CellphoneSettings.BurnerCellScaleformName;
    public string CustomRingtone { get; set; } = "";
    public string CustomTextTone { get; set; } = "";
    public int CustomTheme { get; set; } = -1;
    public int CustomBackground { get; set; } = -1;
    public float CustomVolume { get; set; } = -1.0f;
    public int CustomPhoneType { get; set; } = -1;
    public string CustomPhoneOS { get; set; } = "";
    private bool ShouldCheckScheduledItems => GameTimeLastCheckedScheduledItems == 0 || Game.GameTime - GameTimeLastCheckedScheduledItems >= GameTimeBetweenCheckScheduledItems;
    public bool IsActive => BurnerPhone?.IsActive == true;
    public List<PhoneText> TextList => AddedTexts;
    public List<PhoneContact> ContactList => AddedContacts;
    public List<PhoneResponse> PhoneResponseList => PhoneResponses;
    public CellPhone(ICellPhoneable player, IContactInteractable gangInteractable, IInventoryable inventory,IJurisdictions jurisdictions, ISettingsProvideable settings, ITimeReportable time, IGangs gangs, IPlacesOfInterest placesOfInterest, IZones zones, IStreets streets,
        IGangTerritories gangTerritories, ICrimes crimes, IEntityProvideable world, IModItems modItems, IWeapons weapons, INameProvideable names, IShopMenus shopMenus, ICellphones cellphones, IContacts contacts, IAgencies agencies)
    {
        Player = player;
        MenuPool = new MenuPool();
        Jurisdictions = jurisdictions;
        Settings = settings;
        Time = time;
        Gangs = gangs;
        Zones = zones;
        Streets = streets;
        ModItems = modItems;
        ContactIndex = 0;
        PlacesOfInterest = placesOfInterest;
        GangTerritories = gangTerritories;
        ContactInteractable = gangInteractable;
        Crimes = crimes;
        World = world;
        Weapons = weapons;
        Names = names;
        ShopMenus = shopMenus;
        Contacts = contacts;
        Agencies = agencies;
        BurnerPhone = new BurnerPhone(Player, Time, Settings, modItems, Contacts);
        phoneAudioPlayer = new NAudioPlayer(Settings);
        Cellphones = cellphones;
        CurrentCellphoneData = Cellphones.GetDefault();
        Inventory = inventory;
    }
    public void Setup()
    {
        IsDisposed = false;
        foreach (PhoneContact phoneContact in Contacts.GetDefaultContacts())
        {
            AddContact(phoneContact, false);
        }
        BurnerPhone.Setup();
        phoneAudioPlayer.Setup();

        // Initialize drug request timer
        DrugRequestTimer = new System.Timers.Timer(30000); // 1 minute (60000 ms)
        DrugRequestTimer.Elapsed += OnDrugRequestTimerElapsed;
        DrugRequestTimer.AutoReset = true;
        DrugRequestTimer.Start();
    }

    private void OnDrugRequestTimerElapsed(object sender, ElapsedEventArgs e)
    {
        if (IsDisposed || IsDisposingDrugRequestTimer)
            return;

        // 100% chance to generate a drug request
        SendRandomDrugRequest();
    }

    private void SendRandomDrugRequest()
    {
        try
        {
            // Get player's inventory drugs
            var playerDrugs = Inventory?.Inventory?.ItemsList
                .Where(item => item.ModItem.ItemType == ItemType.Drugs ||
                              item.ModItem.ItemSubType == ItemSubType.Narcotic)
                .Where(item => item.RemainingPercent >= 1.0f) // Only consider drugs with at least 1 unit
                .Where(item => item.ModItem.IsPublicUseIllegal) // Only include illegal items for drug deals
                .ToList();

            if (playerDrugs == null || !playerDrugs.Any())
            {
                EntryPoint.WriteToConsole("No illegal drug items found in player's inventory for customer request");
                return;
            }

            // Pick a random drug from the player's inventory
            var inventoryItem = playerDrugs.PickRandom();
            ModItem requestedDrug = inventoryItem.ModItem;

            // Pick a random customer name and create a contact
            string customerName = DrugRequestCustomerNames.PickRandom();
            string customerIcon = "CHAR_BLANK_ENTRY";

            // Create a custom message with the drug name
            string message = DrugRequestMessages.PickRandom() + $" Looking for {requestedDrug.Name}.";

            // Create a drug customer phone contact
            PhoneContact drugCustomer = new PhoneContact(customerName, customerIcon);

            // Add a text message from this customer
            AddText(customerName, customerIcon, message, Time.CurrentHour, Time.CurrentMinute, false, null);

            // Display notification and play sound
            NativeHelper.DisplayNotificationCustom(customerIcon, customerIcon, customerName, "~g~Text Received~s~", message, NotificationIconTypes.ChatBox, false);
            PlayTexttone();

            // Add contact if not exists
            AddContact(drugCustomer, false);

            // Show response prompt
            ShowDrugRequestPrompt(drugCustomer, requestedDrug, message);

            EntryPoint.WriteToConsole($"Drug request sent from {customerName} for {requestedDrug.Name}");
        }
        catch (Exception ex)
        {
            EntryPoint.WriteToConsole($"Error sending drug request: {ex.Message}", 0);
        }
    }


    private void ShowDrugRequestPrompt(PhoneContact contact, ModItem requestedDrug, string message)
    {
        GameFiber.StartNew(delegate
        {
            try
            {
                // Create a dialog menu for the drug request
                UIMenu drugRequestMenu = new UIMenu("Drug Request", $"{contact.Name} wants {requestedDrug.Name}");
                drugRequestMenu.SetBannerType(EntryPoint.LSRedColor);

                // Create menu items for accept and decline
                UIMenuItem acceptButton = new UIMenuItem("Accept Request", "Agree to sell drugs to this customer");
                UIMenuItem declineButton = new UIMenuItem("Decline Request", "Tell them you're not interested");

                // Add items to menu
                drugRequestMenu.AddItem(acceptButton);
                drugRequestMenu.AddItem(declineButton);

                // Handle menu selection
                drugRequestMenu.OnItemSelect += (sender, item, index) =>
                {
                    if (item == acceptButton)
                    {
                        // Accept the drug request
                        string responseMessage = $"Yeah, I got some {requestedDrug.Name}. Let's meet up.";
                        AddPhoneResponse(contact.Name, contact.IconName, responseMessage);

                        // Create a follow-up message with meeting location
                        GameFiber.Sleep(2000); // Delay before response

                        string streetName = Streets.GetStreetNames(Game.LocalPlayer.Character.Position, false);
                        Zone currentZone = Zones.GetZone(Game.LocalPlayer.Character.Position);
                        string zoneName = currentZone != null ? currentZone.DisplayName : "San Andreas";

                        string meetupMessage = $"Great. Meet me near {streetName} in {zoneName} in about 10 minutes.";

                        AddScheduledText(contact, meetupMessage, 1, true);

                        EntryPoint.WriteToConsole($"Drug deal accepted with {contact.Name} for {requestedDrug.Name}");
                    }
                    else if (item == declineButton)
                    {
                        // Decline the drug request
                        string responseMessage = "Sorry, I can't help you right now.";
                        AddPhoneResponse(contact.Name, contact.IconName, responseMessage);

                        EntryPoint.WriteToConsole($"Drug request from {contact.Name} declined");
                    }

                    // Close the menu
                    sender.Visible = false;
                };

                // Add this menu to your menu pool and show it
                MenuPool.Add(drugRequestMenu);
                drugRequestMenu.Visible = true;

                // The menu will be processed in your existing Update method's MenuPool.ProcessMenus() call
            }
            catch (Exception ex)
            {
                EntryPoint.WriteToConsole($"Error showing drug request prompt: {ex.Message}", 0);
            }
        }, "DrugRequestPrompt");
    }

    private void SetupDrugDealMeeting(PhoneContact contact, ModItem drugItem, string locationName)
    {
        GameFiber.StartNew(delegate
        {
            try
            {
                // Create a meeting location near the player but not too close
                Vector3 playerPosition = Game.LocalPlayer.Character.Position;
                Vector3 meetingLocation = GetNearbyMeetingLocation(playerPosition);

                // Create a blip on the map
                Blip dealBlip = new Blip(meetingLocation)
                {
                    Color = System.Drawing.Color.LightGreen,
                    Scale = 0.8f,
                    Name = $"Drug Deal: {contact.Name}"
                };

                // Store the blip so we can remove it later
                if (ActiveDrugDealBlips.ContainsKey(contact.Name))
                {
                    if (ActiveDrugDealBlips[contact.Name] != null && ActiveDrugDealBlips[contact.Name].Exists())
                        ActiveDrugDealBlips[contact.Name].Delete();

                    ActiveDrugDealBlips[contact.Name] = dealBlip;
                }
                else
                {
                    ActiveDrugDealBlips.Add(contact.Name, dealBlip);
                }

                // Add the blip to the world entity
                World.AddBlip(dealBlip);

                // Notify the player - Replace with standard notification since custom button notification isn't available
                Game.DisplayNotification("CHAR_DEFAULT", "CHAR_DEFAULT", "Drug Deal",
                    $"Meeting with {contact.Name}",
                    $"The location for your drug deal has been marked on your map.");

                // Create a customer ped and wait for player to arrive
                GameFiber.Sleep(2000);

                // Add buyer to the meeting point
                PedExt buyer = SpawnDrugBuyer(meetingLocation, contact.Name);
                if (buyer != null)
                {
                    // Wait for player to arrive or timeout
                    int timeout = 0;
                    const int TIMEOUT_MAX = 1800; // 30 minutes of game time (60 seconds per minute)
                    bool playerArrived = false;

                    while (timeout < TIMEOUT_MAX)
                    {
                        // Check if the buyer still exists and is alive
                        if (buyer.Pedestrian == null || !buyer.Pedestrian.Exists() || buyer.Pedestrian.IsDead)
                        {
                            // Customer was killed or disappeared
                            CancelDrugDeal(contact, "Your contact was killed or has fled the area.", true);
                            return;
                        }

                        float distance = Vector3.Distance(Game.LocalPlayer.Character.Position, meetingLocation);
                        if (distance < 10f)
                        {
                            playerArrived = true;
                            break;
                        }

                        GameFiber.Sleep(1000);
                        timeout++;
                    }

                    if (playerArrived)
                    {
                        // Handle transaction
                        HandleDrugTransaction(contact, buyer, drugItem);
                    }
                    else
                    {
                        // Player didn't show up in time
                        CancelDrugDeal(contact, "You missed your meeting. The buyer has left.", true);
                        // Clean up buyer
                        if (buyer.Pedestrian != null && buyer.Pedestrian.Exists())
                            buyer.Pedestrian.Delete();
                    }
                }
                else
                {
                    CancelDrugDeal(contact, "Unable to arrange the meeting. The buyer didn't show up.", false);
                }

            }
            catch (Exception ex)
            {
                EntryPoint.WriteToConsole($"Error in drug deal setup: {ex.Message}", 0);
                if (ActiveDrugDealBlips.ContainsKey(contact.Name))
                {
                    if (ActiveDrugDealBlips[contact.Name] != null && ActiveDrugDealBlips[contact.Name].Exists())
                        ActiveDrugDealBlips[contact.Name].Delete();
                }
            }
        }, "DrugDealMeetingSetup");
    }

    private Vector3 GetNearbyMeetingLocation(Vector3 playerPosition)
    {
        // Try to find a suitable location 100-300 meters away from the player
        // Start with a random direction
        float angle = (float)(new Random().NextDouble() * Math.PI * 2);
        float distance = new Random().Next(100, 300);

        Vector3 offset = new Vector3(
            (float)Math.Sin(angle) * distance,
            (float)Math.Cos(angle) * distance,
            0f);

        Vector3 targetPos = playerPosition + offset;

        // Make sure the position is on the ground
        float groundZ;
        NativeFunction.Natives.GET_GROUND_Z_FOR_3D_COORD(targetPos.X, targetPos.Y, 1000f, out groundZ, false);

        return new Vector3(targetPos.X, targetPos.Y, groundZ);
    }

    private PedExt SpawnDrugBuyer(Vector3 location, string contactName)
    {
        try
        {
            // Get a random civilian model
            string[] civilianModels = new string[]
            {
            "a_m_m_bevhills_01",
            "a_m_y_hipster_01",
            "a_m_y_hipster_02",
            "a_f_y_hippie_01",
            "a_m_y_mexthug_01",
            "g_m_y_strpunk_01",
            "a_m_y_skater_01",
            "a_m_y_stwhi_01"
            };

            string buyerModel = civilianModels[new Random().Next(civilianModels.Length)];

            // Create a standard Rage ped that we can manage directly
            Ped buyerPed = new Ped(buyerModel, location, 0f);

            // Set a name for identification
            buyerPed.Metadata.buyerName = contactName;

            // Make them stand still
            NativeFunction.Natives.TASK_STAND_STILL(buyerPed, -1);

            // Make them face random directions periodically to look natural
            GameFiber.StartNew(delegate
            {
                try
                {
                    while (buyerPed != null && buyerPed.Exists() && !buyerPed.IsDead)
                    {
                        float randomHeading = new Random().Next(360);
                        buyerPed.Heading = randomHeading;
                        GameFiber.Sleep(new Random().Next(5000, 15000)); // Change direction every 5-15 seconds
                    }
                }
                catch { /* Ignore errors in this thread */ }
            });

            // Instead of creating a PedExt which requires many dependencies,
            // we'll create a simple wrapper class just for this purpose
            return new DrugBuyerPed(buyerPed, contactName);
        }
        catch (Exception ex)
        {
            EntryPoint.WriteToConsole($"Error spawning drug buyer: {ex.Message}", 0);
            return null;
        }
    }

    private void HandleDrugTransaction(PhoneContact contact, PedExt buyer, ModItem drugItem)
    {
        // Set up a simple interaction menu for the transaction
        UIMenu transactionMenu = new UIMenu("Drug Deal", $"Sell drugs to {contact.Name}");
        transactionMenu.SetBannerType(EntryPoint.LSRedColor);

        // Get the drug from inventory
        var drugInInventory = Inventory?.Inventory?.ItemsList
            .FirstOrDefault(item => item.ModItem == drugItem && item.RemainingPercent >= 1.0f);

        if (drugInInventory == null)
        {
            Game.DisplayHelp($"You don't have any {drugItem.Name} left to sell.");
            CancelDrugDeal(contact, $"You don't have the {drugItem.Name} you promised. The deal is off.", true);
            return;
        }

        // Calculate a fair price - avoiding EquipmentItem.Price which isn't available
        int basePrice = 100; // Default price if can't determine from item

        // Add menu items for different quantities
        int maxQuantity = (int)Math.Floor(drugInInventory.RemainingPercent);
        for (int quantity = 1; quantity <= Math.Min(5, maxQuantity); quantity++)
        {
            int dealPrice = basePrice * quantity;
            UIMenuItem quantityItem = new UIMenuItem($"Sell {quantity} {drugItem.Name}", $"${dealPrice}");
            transactionMenu.AddItem(quantityItem);
        }

        UIMenuItem cancelItem = new UIMenuItem("Cancel Deal", "Walk away from the transaction");
        transactionMenu.AddItem(cancelItem);

        // Handle menu selection
        transactionMenu.OnItemSelect += (sender, item, index) =>
        {
            if (item == cancelItem)
            {
                CancelDrugDeal(contact, "You decided not to go through with the deal.", true);
            }
            else
            {
                // Parse quantity from the menu item text
                string quantityText = item.Text.Replace("Sell ", "");
                int quantity = int.Parse(quantityText.Substring(0, quantityText.IndexOf(' ')));

                // Calculate price
                int dealPrice = basePrice * quantity;

                // Process the transaction
                drugInInventory.RemainingPercent -= quantity;
                if (drugInInventory.RemainingPercent <= 0)
                    Inventory.Inventory.ItemsList.Remove(drugInInventory);

                // Give player money - using proper money command if available
                if (Player is ILocationInteractable locPlayer)
                {
                    // Use the BankAccounts to add money properly
                    locPlayer.BankAccounts.GiveMoney(dealPrice, false);
                }

                // Show success notification
                string successMsg = $"You sold {quantity} {drugItem.Name} for ${dealPrice}.";
                Game.DisplayNotification(contact.IconName, contact.IconName, contact.Name, "~g~Deal Completed", successMsg);

                // Send a follow-up text
                string followUpMsg = RandomThanksMessage(drugItem.Name);
                AddScheduledText(contact, followUpMsg, 5, true);

                // Clean up
                if (ActiveDrugDealBlips.ContainsKey(contact.Name) && ActiveDrugDealBlips[contact.Name] != null)
                {
                    ActiveDrugDealBlips[contact.Name].Delete();
                    ActiveDrugDealBlips.Remove(contact.Name);
                }

                // Clean up PedExt properly
                if (buyer != null && buyer.Pedestrian != null && buyer.Pedestrian.Exists())
                    buyer.Pedestrian.Delete();
            }

            // Close the menu
            sender.Visible = false;
        };

        // Add the menu to the pool and show it
        MenuPool.Add(transactionMenu);
        transactionMenu.Visible = true;
    }

    private void CancelDrugDeal(PhoneContact contact, string reason, bool notifyCustomer)
    {
        // Clean up the blip
        if (ActiveDrugDealBlips.ContainsKey(contact.Name) && ActiveDrugDealBlips[contact.Name] != null)
        {
            ActiveDrugDealBlips[contact.Name].Delete();
            ActiveDrugDealBlips.Remove(contact.Name);
        }

        // Notify the player
        Game.DisplayNotification(contact.IconName, contact.IconName, contact.Name, "~r~Deal Canceled", reason);

        // Send an angry text from the customer
        if (notifyCustomer)
        {
            string angryMessage = RandomAngryMessage();
            AddScheduledText(contact, angryMessage, 1, true);
        }
    }

    private string RandomThanksMessage(string drugName)
    {
        List<string> messages = new List<string>
    {
        $"Good stuff! Hit me up if you get more {drugName} in stock.",
        "Perfect, exactly what I needed. Will reach out again soon.",
        "Thanks for coming through. You're reliable, I like that.",
        "This is quality stuff. I'll definitely be a repeat customer.",
        "Nice doing business with you. Let's do this again sometime."
    };

        return messages.PickRandom();
    }

    private string RandomAngryMessage()
    {
        List<string> messages = new List<string>
    {
        "Wtf man? Don't waste my time like this.",
        "Not cool bailing on me. I'll find another dealer.",
        "I waited forever. Don't bother texting me again.",
        "You're unreliable. Guess I'll have to find someone else.",
        "Lost my number. I don't deal with flakes."
    };

        return messages.PickRandom();
    }

    public void ContactAnswered(PhoneContact contact)
    {
        if (!BurnerPhone.IsActive)
        {

        }
        else
        {
            isRunningForcedMobileTask = false;
        }
        contact.OnAnswered(ContactInteractable, this, Gangs, PlacesOfInterest, Settings, Jurisdictions, Crimes, World, ModItems, Weapons, Names, ShopMenus, Agencies);
    }
    public void DeleteText(PhoneText text)
    {
        AddedTexts.Remove(text);
        ReIndexTexts();
    }
    public void DeletePhoneRespone(PhoneResponse phoneResponse)
    {
        PhoneResponses.Remove(phoneResponse);
    }
    private void Update()
    {
        CheckScheduledItems();
        MenuPool.ProcessMenus();
        foreach (PhoneContact phoneContact in ContactList)
        {
            phoneContact.MenuInteraction?.Update();
        }
    }
    public void Reset()
    {
        CustomRingtone = "";
        CustomTextTone = "";
        CustomTheme = -1;
        CustomBackground = -1;
        CustomVolume = -1.0f;
        ContactIndex = 0;
        CurrentCellphoneData = Cellphones.GetDefault();
        AddedTexts = new List<PhoneText>();
        AddedContacts = new List<PhoneContact>();
        PhoneResponses = new List<PhoneResponse>();
        ScheduledContacts = new List<ScheduledContact>();
        ScheduledTexts = new List<ScheduledText>();
        foreach (PhoneContact phoneContact in Contacts.GetDefaultContacts())
        {
            AddContact(phoneContact, false);
        }
    }
    public void ClearTextMessages()
    {
        AddedTexts = new List<PhoneText>();
    }
    public void ClearContacts()
    {
        ContactIndex = 0;
        AddedContacts = new List<PhoneContact>();
    }
    public void ClearPhoneResponses()
    {
        PhoneResponses = new List<PhoneResponse>();
    }

public void Dispose()
{
    IsDisposingDrugRequestTimer = true;
    if (DrugRequestTimer != null)
    {
        DrugRequestTimer.Stop();
        DrugRequestTimer.Elapsed -= OnDrugRequestTimerElapsed;
        DrugRequestTimer.Dispose();
        DrugRequestTimer = null;
    }
    
    if (Settings.SettingsManager.CellphoneSettings.AllowBurnerPhone)
    {
        BurnerPhone.ClosePhone();
    }
    NativeHelper.StartScript("cellphone_flashhand", 1424);
    NativeHelper.StartScript("cellphone_controller", 1424);
    IsDisposed = true;
}
    public void Close(int time)
    {
        if (isRunningForcedMobileTask)
        {
            NativeFunction.Natives.DESTROY_MOBILE_PHONE();
        }
        isRunningForcedMobileTask = false;
        if (Settings.SettingsManager.CellphoneSettings.AllowBurnerPhone && IsActive)
        {
            BurnerPhone.ClosePhone();
        }
        else if (isRunningForcedMobileTask)
        {
            NativeFunction.Natives.DESTROY_MOBILE_PHONE();
        }
    }
    public void OpenBurner()
    {
        if (Settings.SettingsManager.CellphoneSettings.AllowBurnerPhone)
        {
            BurnerPhone.OpenPhone();
        }
    }
    public void CloseBurner()
    {
        if (Settings.SettingsManager.CellphoneSettings.AllowBurnerPhone)
        {
            BurnerPhone.ClosePhone();
        }
    }
    public void AddScamText()
    {
        List<string> ScammerNames = new List<string>() {
        "American Freedom Institute",
        "Lifestyle Unlimited",
        "NAMB Products",
        "Turner Investments",
        "L.F. Fields",
        "Jambog Ltd.",


        };
        List<string> ScammerMessages = new List<string>() {
        "Beautiful weekend coming up. Wanna go out? Sophie gave me your number. Check out my profile here: virus.link/safe",
        "Your IBS tax refund is pending acceptance. Must accept within 24 hours http://asdas.sdf.sdf//asdasd",
        "We've been trying to reach you concerning your vehicle's ~r~extended warranty~s~. You should've received a notice in the mail about your car's extended warranty eligibility.",
        "Verify your Lifeinvader ID. Use Code: DVWB@55",
        "You've won a prize! Go to securelink.safe.biz.ug to claim your ~r~$500~s~ gift card.",
        "Dear customer, Fleeca Bank is closing your bank accounts. Please confirm your pin at fleecascam.ytmd/theft to keep your account activated",
        "URGENT! Your grandson was arrested last night in New Armadillo. Need bail money immediately! Wire Eastern Confederacy at econfed.utg/legit",

        };
        Player.CellPhone.AddScheduledText(new PhoneContact(ScammerNames.PickRandom(), "CHAR_BLANK_ENTRY"), ScammerMessages.PickRandom(), 0, true);
        //CheckScheduledTexts();     

    }
    public void RandomizeSettings()
    {
        var dir = new DirectoryInfo("Plugins\\LosSantosRED\\audio\\tones");
        List<FileInfo> files = dir.GetFiles().ToList();
        if (files != null)
        {
            CustomRingtone = files.PickRandom()?.Name;
            CustomTextTone = files.PickRandom()?.Name;
        }
        else
        {
            CustomRingtone = "";
            CustomTextTone = "";
        }
        CurrentCellphoneData = Cellphones.GetRandomRegular();
        if (CurrentCellphoneData != null)
        {
            CustomTheme = CurrentCellphoneData.GetRandomTheme();
            CustomBackground = CurrentCellphoneData.GetRandomBackground();
            CustomPhoneType = CurrentCellphoneData.PhoneType;// - 1;
            CustomPhoneOS = CurrentCellphoneData.PhoneOS;// "";
        }
        else
        {
            CustomTheme = RandomItems.GetRandomNumberInt(1, 8);
            CustomBackground = new List<int>() { 0, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17 }.PickRandom();
            CustomPhoneType = RandomItems.GetRandomNumberInt(0, 3);
            CustomPhoneOS = CustomPhoneType == 0 ? "cellphone_ifruit" : CustomPhoneType == 1 ? "cellphone_facade" : CustomPhoneType == 2 ? "cellphone_badger" : "cellphone_ifruit";// new List<string>() { "cellphone_ifruit", "cellphone_facade", "cellphone_badger" }.PickRandom();
        }
    }
    private void CheckScheduledItems()
    {
        if (ShouldCheckScheduledItems)
        {
            bool hasDisplayed = CheckScheduledTexts();
            if (!hasDisplayed)
            {
                CheckScheduledContacts();
            }
            GameTimeLastCheckedScheduledItems = Game.GameTime;
        }
    }
    private bool CheckScheduledTexts()
    {
        foreach(ScheduledText scheduledText in ScheduledTexts)
        {
            EntryPoint.WriteToConsole($"I HAVE SCHEDULES TEXTS FROM {scheduledText.ContactName} {scheduledText.Message}");
        }
        for (int i = ScheduledTexts.Count - 1; i >= 0; i--)
        {
            
            ScheduledText sc = ScheduledTexts[i];

            EntryPoint.WriteToConsole($"{sc.ContactName} Text Stuff: {DateTime.Compare(Time.CurrentDateTime, sc.TimeToSend)} Currently:{Time.CurrentDateTime} TimeToSend:{sc.TimeToSend} GameTimeDiff{Game.GameTime - sc.GameTimeSent}");

            if (DateTime.Compare(Time.CurrentDateTime, sc.TimeToSend) >= 0 && (sc.SendImmediately || Game.GameTime - sc.GameTimeSent >= 5000))
            {
                if (!AddedTexts.Any(x => x.ContactName == sc.ContactName && x.Message == sc.Message && sc.TimeToSend.Hour == x.HourSent && sc.TimeToSend.Minute == x.MinuteSent))
                {
                    AddText(sc.ContactName, sc.IconName, sc.Message, Time.CurrentHour, Time.CurrentMinute, false, sc.CustomPicture);

                    if (!string.IsNullOrEmpty(sc.CustomPicture))
                    {
                        EntryPoint.WriteToConsole($"CUSTOM PICTURE SENT {sc.CustomPicture}");
                        NativeHelper.DisplayNotificationCustom(sc.CustomPicture, sc.CustomPicture, sc.ContactName, "~g~Text Received~s~", sc.Message, NotificationIconTypes.ChatBox, false);
                    }
                    else
                    {
                        NativeHelper.DisplayNotificationCustom(sc.IconName, sc.IconName, sc.ContactName, "~g~Text Received~s~", sc.Message, NotificationIconTypes.ChatBox, false);
                    }
                    PlayTexttone();
                    AddContact(sc.PhoneContact, true);
                }
                ScheduledTexts.RemoveAt(i);
                return true;
            }
        }
        return false;
    }
    private void CheckScheduledContacts()
    {
        for (int i = ScheduledContacts.Count - 1; i >= 0; i--)
        {
            ScheduledContact sc = ScheduledContacts[i];
            if (DateTime.Compare(Time.CurrentDateTime, sc.TimeToSend) >= 0 && (sc.SendImmediately || Game.GameTime - sc.GameTimeSent >= 7000))
            {
                if (!AddedContacts.Any(x => x.Name == sc.ContactName))
                {
                    AddContact(sc.PhoneContact, true);
                    PlayTexttone();
                    if (sc.Message == "")
                    {
                        NativeHelper.DisplayNotificationCustom(sc.IconName, sc.IconName, "New Contact", sc.ContactName, NotificationIconTypes.AddFriendRequest, true);
                    }
                    else
                    {
                        NativeHelper.DisplayNotificationCustom(sc.IconName, sc.IconName, "New Contact", sc.ContactName, sc.Message, NotificationIconTypes.AddFriendRequest, false);
                    }
                }
                ScheduledContacts.RemoveAt(i);
            }
        }
    }
    public void AddContact(PhoneContact phoneContact, bool displayNotification)
    {
        if(phoneContact == null)
        {
            EntryPoint.WriteToConsole("AddContact PHONECONTACT IS NULL");

        }

        if (!AddedContacts.Any(x => x.Name == phoneContact.Name))
        {
            phoneContact.Index = ContactIndex;
            ContactIndex++;
            AddedContacts.Add(phoneContact);
            if (displayNotification)
            {
                NativeHelper.DisplayNotificationCustom(phoneContact.IconName, phoneContact.IconName, "New Contact", phoneContact.Name, NotificationIconTypes.AddFriendRequest, true);
                PlayTexttone();
            }
        }
    }



    public void AddScheduledText(PhoneContact phoneContact, string MessageToSend, int minutesToWait, bool sendImmediately)
    {
        AddScheduledText(phoneContact, MessageToSend, Time.CurrentDateTime.AddMinutes(minutesToWait), sendImmediately);
    }
    public void AddScheduledText(PhoneContact phoneContact, string MessageToSend, DateTime timeToAdd, bool sendImmediately)
    {
        EntryPoint.WriteToConsole("AddScheduledText");
        if (phoneContact == null)
        {
            return;
        }
        if (!ScheduledTexts.Any(x => x.ContactName == phoneContact.Name && x.Message == MessageToSend && x.TimeToSend == timeToAdd))
        {
            EntryPoint.WriteToConsole($"AddScheduledText ADD phoneContact{phoneContact.Name} timeToAdd:{timeToAdd} MessageToSend{MessageToSend}");
            ScheduledTexts.Add(new ScheduledText(timeToAdd, phoneContact, MessageToSend) { SendImmediately = sendImmediately });
        }
    }
    public void AddCustomScheduledText(PhoneContact phoneContact, string MessageToSend, DateTime timeToAdd, string customPicture, bool sendImmediately)
    {
        if (phoneContact == null)
        {
            return;
        }
        if (!ScheduledTexts.Any(x => x.ContactName == phoneContact.Name && x.Message == MessageToSend && x.TimeToSend == timeToAdd))
        {
            EntryPoint.WriteToConsole($"CUSTOM PICTURE SENT {customPicture}");
            EntryPoint.WriteToConsole($"AddScheduledText ADD phoneContact{phoneContact.Name} timeToAdd:{timeToAdd} MessageToSend{MessageToSend}");
            ScheduledTexts.Add(new ScheduledText(timeToAdd, phoneContact, MessageToSend) { CustomPicture = customPicture, SendImmediately = sendImmediately });
        }
    }
    public void AddText(string Name, string IconName, string message, int hourSent, int minuteSent, bool isRead, string customPicture)
    {
        if (!AddedTexts.Any(x => x.ContactName == Name && x.Message == message && x.HourSent == hourSent && x.MinuteSent == minuteSent))
        {
            PhoneText textA = new PhoneText(Name, TextIndex, message, hourSent, minuteSent) { CustomPicture = customPicture };
            textA.IconName = IconName;
            textA.IsRead = isRead;
            textA.TimeReceived = Time.CurrentDateTime;
            TextIndex++;
            AddedTexts.Add(textA);
            ReIndexTexts();

        }
    }
    public void ReIndexTexts()
    {
        int NewTextIndex = 0;
        foreach (PhoneText ifta in TextList.OrderByDescending(x => x.TimeReceived))
        {
            ifta.Index = NewTextIndex;
            NewTextIndex++;
        }
    }
    public void AddPhoneResponse(string Name, string IconName, string Message)
    {
        PhoneResponses.Add(new PhoneResponse(Name, IconName, Message, Time.CurrentDateTime));
        NativeHelper.DisplayNotificationCustom(IconName, IconName, Name, "~o~Response", Message, NotificationIconTypes.RightJumpingArrow, false);
        PlayPhoneResponseSound();
    }
    public void AddPhoneResponse(string Name, string Message)
    {
        string IconName = ContactList.FirstOrDefault(x => x.Name.ToLower() == Name.ToLower())?.IconName;
        PhoneResponses.Add(new PhoneResponse(Name, IconName, Message, Time.CurrentDateTime));
        NativeHelper.DisplayNotificationCustom(IconName, IconName, Name, "~o~Response", Message, NotificationIconTypes.RightJumpingArrow, false);
        PlayPhoneResponseSound();
    }
    public void DisableContact(string Name)
    {
        PhoneContact myContact = AddedContacts.FirstOrDefault(x => x.Name == Name);
        if (myContact != null)
        {
            myContact.Active = false;
        }
    }
    public bool IsContactEnabled(string contactName)
    {
        PhoneContact myContact = AddedContacts.FirstOrDefault(x => x.Name == contactName);
        if (myContact != null)
        {
            return myContact.Active;
        }
        return false;
    }
    public void StopAudio()
    {
        if (!phoneAudioPlayer.IsAudioPlaying)
        {
            return;
        }
        phoneAudioPlayer.Abort();
    }
    public void PlayRingtone()
    {
        if (SleepMode)
        {
            return;
        }
        float volumeToUse = Volume.Clamp(0.0f, 1.0f);
        string ringToneToUse = Settings.SettingsManager.CellphoneSettings.DefaultCustomRingtoneNameNew;
        if (!string.IsNullOrEmpty(CustomRingtone))
        {
            ringToneToUse = CustomRingtone;
        }
        if (Settings.SettingsManager.CellphoneSettings.UseCustomRingtone)
        {
            string AudioPath = $"tones\\{ringToneToUse}";
            if (!phoneAudioPlayer.IsAudioPlaying)
            {
                phoneAudioPlayer.Play(AudioPath, volumeToUse, false, false);
            }
        }
        else
        {
            NativeFunction.Natives.PLAY_SOUND_FRONTEND(-1, "Text_Arrive_Tone", "Phone_SoundSet_Default", 0);
        }
    }
    public void PlayTexttone()
    {
        if (SleepMode)
        {
            return;
        }
        float volumeToUse = Volume.Clamp(0.0f, 1.0f);
        string textToneToUse = Settings.SettingsManager.CellphoneSettings.DefaultCustomTexttoneNameNew;
        if (!string.IsNullOrEmpty(CustomTextTone))
        {
            textToneToUse = CustomTextTone;
        }
        if (Settings.SettingsManager.CellphoneSettings.UseCustomTexttone)
        {
            string AudioPath = $"tones\\{textToneToUse}";
            if (!phoneAudioPlayer.IsAudioPlaying)
            {
                phoneAudioPlayer.Play(AudioPath, volumeToUse, false, false);
            }
        }
        else
        {
            NativeFunction.Natives.PLAY_SOUND_FRONTEND(-1, "Text_Arrive_Tone", "Phone_SoundSet_Default", 0);
        }
    }
    public void PreviewTextSound()
    {
        GameFiber.StartNew(delegate
        {
            StopAudio();
            GameFiber.Sleep(100);
            if (!phoneAudioPlayer.IsAudioPlaying)
            {
                PlayTexttone();
            }
        }, "PreviewTextSound");
    }
    public void PreviewRingtoneSound()
    {
        GameFiber.StartNew(delegate
        {
            StopAudio();
            GameFiber.Sleep(100);
            if (!phoneAudioPlayer.IsAudioPlaying)
            {
                PlayRingtone();
            }
        }, "PreviewTextSound");
    }
    private void PlayPhoneResponseSound()
    {
        NativeFunction.Natives.PLAY_SOUND_FRONTEND(-1, "Hang_Up", "Phone_SoundSet_Default", 0);
    }
    public void Start()
    {
        GameFiber.StartNew(delegate
        {
            try
            {
                while (!IsDisposed)
                {
                    Update();
                    GameFiber.Yield();
                }
            }
            catch (Exception ex)
            {
                EntryPoint.WriteToConsole(ex.Message + " " + ex.StackTrace, 0);
                EntryPoint.ModController.CrashUnload();
            }
        }, "CellPhone");
        GameFiber.StartNew(delegate
        {
            try
            {
                while (!IsDisposed)
                {
                    if (Settings.SettingsManager.CellphoneSettings.AllowBurnerPhone)
                    {
                        BurnerPhone.Update();
                    }
                    GameFiber.Yield();
                }
            }
            catch (Exception ex)
            {
                EntryPoint.WriteToConsole(ex.Message + " " + ex.StackTrace, 0);
                EntryPoint.ModController.CrashUnload();
            }
        }, "BurnerPhone");
        if (Settings.SettingsManager.CellphoneSettings.TerminateVanillaCellphone)
        {
            NativeFunction.Natives.TERMINATE_ALL_SCRIPTS_WITH_THIS_NAME("cellphone_flashhand");
            NativeFunction.Natives.TERMINATE_ALL_SCRIPTS_WITH_THIS_NAME("cellphone_controller");
        }
    }
    public PhoneContact GetCorruptCopContact()
    {
        if (Contacts.PossibleContacts.CorruptCopContact == null)
        {
            return null;
        }
        return ContactList.FirstOrDefault(x => Contacts.PossibleContacts.CorruptCopContact.Name == x.Name);
    }
    public void ClearPendingTexts()
    {
        ScheduledTexts.Clear();
        ScheduledContacts.Clear();
    }
    public void ClearPendingGangTexts(Gang gang)
    {
        if(gang == null)
        {
            return;
        }
        ScheduledTexts.RemoveAll(x => x.ContactName == gang.ContactName);
        ScheduledContacts.RemoveAll(x => x.ContactName == gang.ContactName);
    }
    public bool HasPendingItems(PhoneContact phoneContact)
    {
        if (phoneContact == null)
        {
            return false;
        }
        return ScheduledTexts.Any(x => x.ContactName == phoneContact.Name) || ScheduledContacts.Any(x => x.ContactName == phoneContact.Name);
    }
    public void ClearPendingTexts(PhoneContact phoneContact)
    {
        if (phoneContact == null)
        {
            return;
        }
        ScheduledTexts.RemoveAll(x => x.ContactName == phoneContact.Name);
        ScheduledContacts.RemoveAll(x => x.ContactName == phoneContact.Name);
    }

    public CorruptCopContact DefaultCorruptCopContact
    {
        get
        {
            return Contacts.PossibleContacts.CorruptCopContact;
        }
    }
    private class ScheduledContact
    {
        public ScheduledContact(DateTime timeToSend, PhoneContact phoneContact, string message)
        {
            TimeToSend = timeToSend;
            ContactName = phoneContact.Name;
            Message = message;
            IconName = phoneContact.IconName;
            GameTimeSent = Game.GameTime;
            PhoneContact = phoneContact;
        }
        public DateTime TimeToSend { get; set; }
        public string ContactName { get; set; }
        public string Message { get; set; } = "We need to talk";
        public string IconName { get; set; } = "CHAR_BLANK_ENTRY";
        public uint GameTimeSent { get; set; }
        public PhoneContact PhoneContact { get; set; }
        public bool SendImmediately { get; set; } = false;
    }
    private class ScheduledText
    {
        public ScheduledText(DateTime timeToSend, PhoneContact phoneContact, string message)
        {
            TimeToSend = timeToSend;
            ContactName = phoneContact.Name;
            Message = message;
            IconName = phoneContact.IconName;
            GameTimeSent = Game.GameTime;
            PhoneContact = phoneContact;
        }
        public DateTime TimeToSend { get; set; }
        public string ContactName { get; set; }
        public string Message { get; set; } = "We need to talk";
        public string IconName { get; set; } = "CHAR_BLANK_ENTRY";
        public uint GameTimeSent { get; set; }
        public PhoneContact PhoneContact { get; set; }
        public string CustomPicture { get; set; }
        public bool SendImmediately { get; set; } = false;
    }

}