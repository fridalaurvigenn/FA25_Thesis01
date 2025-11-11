using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class JournalUIController : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject journalPanel; // Whole book/journal

    [Header("Tabs")]
    [SerializeField] private Button[] tabButtons;
    [SerializeField] private GameObject[] tabPageGroups;

    [Header("Input")]
    [SerializeField] private KeyCode toggleKey   = KeyCode.Tab;
    [SerializeField] private KeyCode prevTabKey  = KeyCode.Q;
    [SerializeField] private KeyCode nextTabKey  = KeyCode.E;
    [SerializeField] private KeyCode prevPageKey = KeyCode.A;
    [SerializeField] private KeyCode nextPageKey = KeyCode.D;

    // --- New Entry Notification ---
    [Header("New Entry UI")]
    [SerializeField] private GameObject newEntryHintPanel;
    [SerializeField] private TextMeshProUGUI newEntryHintText;

    [Header("Tab Visuals")]
    [SerializeField] private float inactiveTabY = -78f;
    [SerializeField] private float activeTabY = -68f;

    // internal state for "jump to new entry"
    private bool hasPendingJump = false;
    private int pendingTabIndex = 0;
    private int pendingPageIndex = 0;

    // Tab indices in tabButtons / tabPageGroups
    // (e.g. 0 = Menu, 1 = Before, 2 = Reflections, 3 = Rituals)
    [SerializeField] private int reflectionsTabIndex = 2;

    private bool isOpen = false;
    private int currentTab = 0;
    private int[] currentPageIndex;

    // Makes sure Elise reflection hint/jump only happens once
    private bool eliseReflectionUnlocked = false;
    private Coroutine hideHintCoroutine = null;

    private void Awake()
    {
        currentPageIndex = new int[tabPageGroups.Length];

        // Bookmark button click hookup
        for (int i = 0; i < tabButtons.Length; i++)
        {
            int tabIndex = i;
            if (tabButtons[i] != null)
            {
                tabButtons[i].onClick.RemoveAllListeners();
                tabButtons[i].onClick.AddListener(() => ShowTab(tabIndex));
            }
        }
    }

    private void Start()
    {
        if (journalPanel != null)
            journalPanel.SetActive(false);

        if (newEntryHintPanel != null)
            newEntryHintPanel.SetActive(false);

        ShowTab(0);
    }

    private void Update()
    {
        // Toggle journal
        if (Input.GetKeyDown(toggleKey))
        {
            // If we have a pending "jump to new entry", do that once
            if (!isOpen && hasPendingJump)
            {
                OpenJournalAtPendingEntry();
            }
            else
            {
                ToggleJournal();
            }
        }

        if (!isOpen) return;

        // Tab switching
        if (Input.GetKeyDown(prevTabKey)) PreviousTab();
        if (Input.GetKeyDown(nextTabKey)) NextTab();

        // Page flipping (2 at a time)
        if (Input.GetKeyDown(prevPageKey)) PreviousPage();
        if (Input.GetKeyDown(nextPageKey)) NextPage();
    }

    // ---------------------- OPEN / CLOSE ----------------------

    private void ToggleJournal()
    {
        isOpen = !isOpen;

        if (journalPanel != null)
            journalPanel.SetActive(isOpen);

        // Freeze / unfreeze player movement
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            var pm = player.GetComponent<PlayerMovement>();
            if (pm != null)
                pm.FreezeMovement(isOpen);
        }

        if (isOpen)
        {
            // Re-show current tab spread when opening
            ShowTab(currentTab);
        }
    }

    private void OpenJournalAtPendingEntry()
    {
        isOpen = true;

        if (journalPanel != null)
            journalPanel.SetActive(true);

        // Freeze player
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            var pm = player.GetComponent<PlayerMovement>();
            if (pm != null)
                pm.FreezeMovement(true);
        }

        // Go straight to the pending tab
        ShowTab(pendingTabIndex);

        // pendingPageIndex is a single-page index; we want the spread containing it
        int spreadLeftIndex = pendingPageIndex - (pendingPageIndex % 2);
        currentPageIndex[pendingTabIndex] = spreadLeftIndex;
        ShowPageSpread(pendingTabIndex);

        // Clear notification and jump state
        hasPendingJump = false;

        if (newEntryHintPanel != null)
            newEntryHintPanel.SetActive(false);

        // Also stop any running hide coroutine, since we already opened it
        if (hideHintCoroutine != null)
        {
            StopCoroutine(hideHintCoroutine);
            hideHintCoroutine = null;
        }
    }

    // ---------------------- TABS ----------------------

    private void ShowTab(int tabIndex)
    {
        currentTab = Mathf.Clamp(tabIndex, 0, tabPageGroups.Length - 1);

        // Enable only the selected tab's page group
        for (int i = 0; i < tabPageGroups.Length; i++)
            tabPageGroups[i].SetActive(i == currentTab);

        // Move bookmarks up/down based on which tab is active
        for (int i = 0; i < tabButtons.Length; i++)
        {
            if (tabButtons[i] == null) continue;

            RectTransform rt = tabButtons[i].GetComponent<RectTransform>();
            if (rt == null) continue;

            Vector2 pos = rt.anchoredPosition;
            pos.y = (i == currentTab) ? activeTabY : inactiveTabY;
            rt.anchoredPosition = pos;
        }

        // Reset if out of range
        if (currentPageIndex[currentTab] < 0)
            currentPageIndex[currentTab] = 0;

        ShowPageSpread(currentTab);
    }

    private void PreviousTab()
    {
        if (tabButtons == null || tabButtons.Length == 0) return;

        int newTab = currentTab - 1;
        if (newTab < 0) newTab = tabButtons.Length - 1;
        ShowTab(newTab);
    }

    private void NextTab()
    {
        if (tabButtons == null || tabButtons.Length == 0) return;

        int newTab = currentTab + 1;
        if (newTab >= tabButtons.Length) newTab = 0;
        ShowTab(newTab);
    }

    // ---------------------- PAGES (2-PAGE SPREAD) ----------------------

    private void ShowPageSpread(int tabIndex)
    {
        if (tabIndex < 0 || tabIndex >= tabPageGroups.Length) return;

        GameObject groupObj = tabPageGroups[tabIndex];
        if (groupObj == null) return;

        Transform group = groupObj.transform;
        int childCount = group.childCount;

        // Hide all pages
        for (int i = 0; i < childCount; i++)
            group.GetChild(i).gameObject.SetActive(false);

        int leftIndex = Mathf.Clamp(currentPageIndex[tabIndex], 0, Mathf.Max(childCount - 1, 0));
        int rightIndex = leftIndex + 1;

        // Left page
        if (leftIndex < childCount)
            group.GetChild(leftIndex).gameObject.SetActive(true);

        // Right page (optional)
        if (rightIndex < childCount)
            group.GetChild(rightIndex).gameObject.SetActive(true);
    }

    private void NextPage()
    {
        if (currentTab < 0 || currentTab >= tabPageGroups.Length) return;
        GameObject groupObj = tabPageGroups[currentTab];
        if (groupObj == null) return;

        Transform group = groupObj.transform;
        int childCount = group.childCount;

        int newIndex = currentPageIndex[currentTab] + 2;

        if (newIndex < childCount)
        {
            currentPageIndex[currentTab] = newIndex;
            ShowPageSpread(currentTab);
        }
    }

    private void PreviousPage()
    {
        int newIndex = currentPageIndex[currentTab] - 2;

        if (newIndex >= 0)
        {
            currentPageIndex[currentTab] = newIndex;
            ShowPageSpread(currentTab);
        }
    }

    // ---------- GENERIC "NEW ENTRY" HANDLER (OPTIONAL FOR FUTURE NPCs) ----------

    public void NotifyNewEntry(GameObject pageObj, string message, int tabIndexOverride = -1)
    {
        if (pageObj == null) return;

        // Decide which tab this entry lives in
        int tabIndex = (tabIndexOverride >= 0) ? tabIndexOverride : reflectionsTabIndex;

        if (tabIndex < 0 || tabIndex >= tabPageGroups.Length) return;
        GameObject groupObj = tabPageGroups[tabIndex];
        if (groupObj == null) return;

        // Make sure the page is active so it can be shown
        pageObj.SetActive(true);

        // Find its page index within that tab's group
        Transform group = groupObj.transform;
        int pageIndex = pageObj.transform.GetSiblingIndex();

        // Save where we should jump when player next presses Tab
        pendingTabIndex = tabIndex;
        pendingPageIndex = pageIndex;
        hasPendingJump = true;

        // Show the little "Press Tab" notification
        if (newEntryHintPanel != null)
            newEntryHintPanel.SetActive(true);

        if (newEntryHintText != null && !string.IsNullOrEmpty(message))
            newEntryHintText.text = message;

        // Optionally start auto-hide here too if you use this method
        if (hideHintCoroutine != null)
            StopCoroutine(hideHintCoroutine);
        hideHintCoroutine = StartCoroutine(HideHintAfterDelay(5f));
    }

    // ---------- SPECIAL: ELISE REFLECTION (ONE-TIME ONLY) ----------

    public void UnlockEliseReflection()
    {
        // 1) Only do this the first time ever (per playthrough)
        if (eliseReflectionUnlocked)
            return;

        eliseReflectionUnlocked = true;

        // 2) We know Elise's reflection is in the Reflections tab, page 0
        int elisePageIndex = 0; // first page in Reflections

        hasPendingJump  = true;
        pendingTabIndex = reflectionsTabIndex;
        pendingPageIndex = elisePageIndex;

        // 3) Show the hint UI
        if (newEntryHintText != null)
            newEntryHintText.text = "Press [Tab] to open Journal - New Entry!";

        if (newEntryHintPanel != null)
            newEntryHintPanel.SetActive(true);

        // 4) Start / restart the auto-hide timer
        if (hideHintCoroutine != null)
            StopCoroutine(hideHintCoroutine);

        hideHintCoroutine = StartCoroutine(HideHintAfterDelay(5f));
    }

    private IEnumerator HideHintAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        // Only auto-hide if the journal is still closed
        if (!isOpen)
        {
            // Cancel the pending jump so Tab goes back to "normal open"
            hasPendingJump  = false;
            pendingTabIndex = 0;
            pendingPageIndex = 0;

            if (newEntryHintPanel != null)
                newEntryHintPanel.SetActive(false);
        }

        hideHintCoroutine = null;
    }
}