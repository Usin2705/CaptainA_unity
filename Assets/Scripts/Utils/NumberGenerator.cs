using UnityEngine;

public static class NumberGenerator
{

    public static int GenerateNumber(NGTaskType taskType)
    {
        switch (taskType)
        {
            case NGTaskType.EASY:
                return Random.Range(0, 11); // [0, 10]

            case NGTaskType.MEDIUM:
                return Random.Range(11, 100); // [11, 99]

            case NGTaskType.HARD:
                return GenerateHardNumber();

            case NGTaskType.RANK:
                return GenerateRankNumber();

            default:
                Debug.LogWarning("Unknown TaskType passed to NumberGenerator");
                return 0;
        }
    }

    private static int GenerateHardNumber()
    {
        int number;
        do
        {
            number = Random.Range(100, 1000); // [100, 999]
        }
        while (!IsValidHardNumber(number));
        return number;
    }

    private static int GenerateRankNumber()
    {
        int number;
        do
        {
            number = Random.Range(11, 1000); // [11, 999]
        }
        while (!IsValidRankNumber(number));
        return number;
    }

    private static bool IsValidHardNumber(int number)
    {
        // Only check 3-digit numbers
        if (number >= 100)
        {
            int secondDigit = (number / 10) % 10;
            return secondDigit != 0;
        }
        return false;
    }

    private static bool IsValidRankNumber(int number)
    {
        if (number >= 100)
        {
            int secondDigit = (number / 10) % 10;
            return secondDigit != 0;
        }
        return true; // 2-digit numbers are always valid for RANK
    }
}

