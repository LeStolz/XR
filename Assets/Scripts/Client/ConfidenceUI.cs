using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ConfidenceUI : MonoBehaviour
{
    [SerializeField] Button submitButton;
    [SerializeField] TextMeshProUGUI submitButtonText;
    [SerializeField] List<Button> buttons;
    [SerializeField] Sprite defaultSprite;
    [SerializeField] Sprite selectedSprite;
    int selectedButtonIndex = -1;

    void Start()
    {
        gameObject.SetActive(false);

        for (int i = 0; i < buttons.Count; i++)
        {
            var currentButton = buttons[i];
            var currentIndex = i;

            currentButton.onClick.AddListener(() =>
            {
                foreach (Button button in buttons)
                {
                    button.image.sprite = defaultSprite;
                }

                currentButton.image.sprite = selectedSprite;
                selectedButtonIndex = currentIndex;
            });
        }

        submitButton.onClick.AddListener(() =>
        {
            if (selectedButtonIndex == -1) return;

            submitButtonText.text = "Syncing...";
            submitButton.interactable = false;

            ClientManager.Singleton.SubmitAnswer(selectedButtonIndex);
        });

        ClientManager.Singleton.OnTrialInit += OnTrialInit;
        ClientManager.Singleton.OnConditionEnd += OnConditionEnd;
        ClientManager.Singleton.OnConfidenceSelect += OnConfidenceSelect;
    }

    void OnTrialInit()
    {
        gameObject.SetActive(false);

        foreach (Button button in buttons)
        {
            button.image.sprite = defaultSprite;
        }

        selectedButtonIndex = -1;

        submitButtonText.text = "Submit";
        submitButton.interactable = true;
    }

    void OnConditionEnd()
    {
        gameObject.SetActive(false);
    }

    void OnConfidenceSelect()
    {
        gameObject.SetActive(true);
    }

    void OnDestroy()
    {
        ClientManager.Singleton.OnTrialInit -= OnTrialInit;
        ClientManager.Singleton.OnConditionEnd -= OnConditionEnd;
        ClientManager.Singleton.OnConfidenceSelect -= OnConfidenceSelect;
    }
}
