﻿using System.Collections;
using System.Threading.Tasks;
using Web3sdks;
using UnityEngine;

[RequireComponent(typeof (Collider))]
public class LootBox : MonoBehaviour
{
    public Animator cameraAnimator;

    public GameObject lootBox;

    public GameObject lootBoxFractured;

    public GameObject redGem;

    public GameObject purpleGem;

    private GameObject fracturedObject;

    private GameObject loot;

    private Animator animator;

    private RaycastHit hit;

    private Ray ray;

    private Web3sdksSDK sdk;

    private ERC1155Reward openedLootItem;

    public GameObject helperText;

    public bool triggerToggle;

    async Task<Pack> GetPackContract()
    {
        await EnsureCorrectWalletState();
        return sdk
            .GetContract("0xd8Bd34726814855fB9cFF58fe5372558e3B411Cb")
            .pack;
    }

    async Task<string> EnsureCorrectWalletState()
    {
        string address =
            await sdk
                .wallet
                .Connect(new WalletConnection()
                {
                    provider = WalletProvider.CoinbaseWallet, // Use Coinbase Wallet
                    chainId = 420 // Switch the wallet Goerli network on connection
                });
        return address;
    }

    async Task<TransactionResult> BuyPackFromMarketplace()
    {
        await EnsureCorrectWalletState();

        UpdateHelperText("Purchasing pack from marketplace...");

        Marketplace marketplace =
            sdk
                .GetContract("0x8ecE57a92ea312D5f31E39E5F6f3E6fC02507D7B")
                .marketplace;

        var result = await marketplace.BuyListing("0", 1);

        UpdateHelperText("Purchase complete! Opening pack now...");

        return result;
    }

    async Task<ERC1155Reward> OpenPack()
    {
        await EnsureCorrectWalletState();
        await BuyPackFromMarketplace();
        Pack packContract = await GetPackContract();

        // Here, 0 is the pack ID, and 1 is the amount of packs to open
        var result = await packContract.Open("0", "1");
        openedLootItem = result.erc1155Rewards[0];
        return result.erc1155Rewards[0];
    }

    void Start()
    {
        // Instantiate the SDK with Gasless Transactions
        sdk =
            new Web3sdksSDK("optimism-goerli",
                new Web3sdksSDK.Options()
                {
                    gasless =
                        new Web3sdksSDK.GaslessOptions()
                        {
                            openzeppelin =
                                new Web3sdksSDK.OZDefenderOptions()
                                {
                                    relayerUrl =
                                        "https://api.defender.openzeppelin.com/autotasks/c2e9a6ca-f2e8-4521-926b-1f9daec2dcb8/runs/webhook/826a5b67-d55d-49dc-8651-5db958ba22b2/DPtceJtayVGgKSDejaFnWk"
                                }
                        }
                });

        animator = GetComponent<Animator>();
    }

    async void Update()
    {
        ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out hit))
        {
            if (hit.transform.name == gameObject.name)
            {
                animator.SetBool("Idle", false);
                animator.SetBool("Hover", true);

                cameraAnimator.SetBool("Idle", false);
                cameraAnimator.SetBool("Hover", true);

                if (Input.GetMouseButtonDown(0) && !triggerToggle)
                {
                    cameraAnimator.SetBool("Hover", true);
                    triggerToggle = true;
                    var openedPack = await OpenPack();
                    triggerToggle = false;
                    if (openedLootItem != null)
                    {
                        animator.SetBool("Open", true);
                        cameraAnimator.SetBool("Open", true);
                    }
                }
            }
            else
            {
                animator.SetBool("Idle", true);
                animator.SetBool("Hover", false);

                cameraAnimator.SetBool("Idle", true);
                cameraAnimator.SetBool("Hover", false);
            }
        }
    }

    //called via animation
    public void LootReward()
    {
        lootBox.SetActive(false);

        if (openedLootItem.tokenId == "0")
        {
            loot = Instantiate(redGem);
            UpdateHelperText("You opened a COMMON red gem. Click the restart icon to play again.");
        }

        if (openedLootItem.tokenId == "1")
        {
            loot = Instantiate(purpleGem);
            UpdateHelperText("You opened a RARE purple gem! Click the restart icon to play again.");
        }

        if (lootBoxFractured != null)
        {
            fracturedObject = Instantiate(lootBoxFractured) as GameObject;
        }
    }

    private void UpdateHelperText(string text)
    {
        helperText.GetComponent<TMPro.TextMeshProUGUI>().text = text;
    }

    public void Restart()
    {
        animator.Rebind();
        animator.SetBool("Idle", true);
        animator.SetBool("Open", false);
        animator.SetBool("Hover", false);

        cameraAnimator.Rebind();
        cameraAnimator.SetBool("Idle", true);
        cameraAnimator.SetBool("Open", false);
        cameraAnimator.SetBool("Hover", false);

        Destroy (loot);
        Destroy (fracturedObject);

        lootBox.SetActive(true);

        openedLootItem = null;
    }

    IEnumerator RestartCo()
    {
        yield return new WaitForFixedUpdate();
    }
}
