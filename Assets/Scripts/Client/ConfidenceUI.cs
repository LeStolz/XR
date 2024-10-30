using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ConfidenceUI : MonoBehaviour
{
    [SerializeField] private Button submitButton;
    [SerializeField] private TextMeshProUGUI submitButtonText;
    [SerializeField] private List<Button> buttons;
    [SerializeField] private Sprite defaultSprite;
    [SerializeField] private Sprite selectedSprite;
    public int SelectedButtonIndex { get; private set; } = -1;

    void Start()
    {
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
                SelectedButtonIndex = currentIndex;
            });
        }

        submitButton.onClick.AddListener(() =>
        {
            if (SelectedButtonIndex == -1) return;

            submitButtonText.text = "Syncing...";
            submitButton.interactable = false;

            ClientManager.Singleton.SubmitAnswer();
        });
    }

    public void Reset()
    {
        foreach (Button button in buttons)
        {
            button.image.sprite = defaultSprite;
        }

        SelectedButtonIndex = -1;

        submitButtonText.text = "Submit";
        submitButton.interactable = true;
    }
}
