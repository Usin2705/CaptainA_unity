/*
*   Part of the algorithm is based on the SuperMemo 2 algorithm.
*   The algorithm is described here: https://www.supermemo.com/english/ol/sm2.htm
*
*   Part of the algorithm is also based on Anki algorithm.
*   The algorithm is described here: https://apps.ankiweb.net/docs/manual.html#what-spaced-repetition-algorithm-does-anki-use
*   and here: https://github.com/ankitects/anki/blob/main/pylib/anki/scheduler/v3.py
*   and here: https://github.com/ankitects/anki/blob/main/pylib/anki/scheduler/v2.py
*   with License: GNU Affero General Public License v3 or later
*/

using System;

public class SM2A
{
    public int Repetition { get; private set; }
    public float EasinessFactor { get; private set; }
    public int Interval { get; private set; }
    
    public SM2A()
    {
        Repetition = 0;
        EasinessFactor = 2.5f;
        Interval = 0;
    }

    public int CalculateInterval(int repetition, float easinessFactor)
    {
        if (repetition == 1)
        {
            return 1;
        }
        else if (repetition == 2)
        {
            return 6;
        }
        else
        {
            return Math.Max((int)(easinessFactor * repetition), 1);
        }
    }

    public void Update(int quality)
    {
        if (quality < 0 || quality > 5)
        {
            throw new ArgumentException("Quality must be between 0 and 5.");
        }

        EasinessFactor = Math.Max(1.3f, EasinessFactor + (0.1f - (5 - quality) * (0.08f + (5 - quality) * 0.02f)));
        Repetition += 1;
        Interval = CalculateInterval(Repetition, EasinessFactor);
    }
    
}
