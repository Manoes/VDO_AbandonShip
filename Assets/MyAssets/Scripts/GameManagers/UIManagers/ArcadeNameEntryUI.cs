using System;
using UnityEngine;
using UnityEngine.UIElements;

public class ArcadeNameEntryUI : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private int nameLength = 3;
    [SerializeField] private string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
    [SerializeField] private float repeatDelay = 0.25f;
    [SerializeField] private float repeatRate = 0.08f;

    // Visual Elements
    Label nameLabel;
    Button saveButton;

    char[] characters;
    int cursorIndex;
    bool isActive;

    float nextRepeatTime;
    int repeatDirection;

    public event Action<string> OnSubmit;

    void Awake()
    {
        characters = new char[nameLength];
        for (int i = 0; i < nameLength; i++)
            characters[i] = 'A';

        SetActive(false);
    }

    public void BindUI(Label label, Button saveBttn = null)
    {
        nameLabel = label;
        saveBttn = saveButton;

        if (saveBttn != null)
        {
            saveBttn.clicked -= Submit;
            saveBttn.clicked += Submit;
        }

        UpdateVisuals();
    }

    public void SetActive(bool active)
    {
        isActive = active;

        if (isActive)
        {
            cursorIndex = 0;
            nextRepeatTime = 0f;
            repeatDirection = 0;
            UpdateVisuals();
        }
        else
        {
            repeatDirection = 0;
        }
    }

    public string CurrentName => new string(characters);

    void Update()
    {
        if (!isActive) return;

        // Joystick Axes 
        float x = Input.GetAxisRaw("Horizontal");
        float y = Input.GetAxisRaw("Vertical");

        // Move Cursor Left/Right (only on Tap)
        if (Input.GetKeyDown("left") || x < -0.5f && Time.unscaledTime >= nextRepeatTime)
        {
            MoveCursor(-1);
            nextRepeatTime = Time.unscaledTime + 0.2f;
        }
        else if (Input.GetKeyDown("right") || x > 0.5f && Time.unscaledTime >= nextRepeatTime)
        {
            MoveCursor(+1);
            nextRepeatTime = Time.unscaledTime + 0.2f;
        }

        // Change Character Up/Down (with Hold Repeat)
        if (y > 0.5f) RepeatChange(+1);
        else if (y < -0.5f) RepeatChange(-1);
        else repeatDirection = 0;

        // Confirm / Save
        if (Input.GetButtonDown("Jump"))
            MoveCursor(+1);

        if (Input.GetButtonDown("Submit"))
            Submit();
    }

    void RepeatChange(int direction)
    {
        if (repeatDirection != direction)
        {
            repeatDirection = direction;
            nextRepeatTime = Time.unscaledTime + repeatDelay;
            ChangeCharacter(direction);
            return;
        }

        if (Time.unscaledTime >= nextRepeatTime)
        {
            // Repeat            
            nextRepeatTime = Time.unscaledTime + repeatRate;
            ChangeCharacter(direction);
        }
    }

    void MoveCursor(int delta)
    {
        cursorIndex = Mathf.Clamp(cursorIndex + delta, 0, nameLength - 1);
        UpdateVisuals();
    }

    void ChangeCharacter(int direction)
    {
        char currentChar = characters[cursorIndex];
        int charIndex = alphabet.IndexOf(currentChar);
        if (charIndex < 0) charIndex = 0;

        charIndex = (charIndex + direction) % alphabet.Length;
        if (charIndex < 0) charIndex += alphabet.Length;

        characters[cursorIndex] = alphabet[charIndex];
        UpdateVisuals();
    }

    void UpdateVisuals()
    {
        if (nameLabel == null) return;

        string displayName = "";
        for (int i = 0; i < nameLength; i++)
        {
            char c = characters[i];
            if (i == cursorIndex) displayName += $"[{c}]";
            else displayName += $"{c}";
            if (i < nameLength - 1) displayName += " ";
        }
        nameLabel.text = displayName;
    }

    void Submit()
    {
        if (!isActive) return;
        OnSubmit?.Invoke(new string(characters));
    }
}