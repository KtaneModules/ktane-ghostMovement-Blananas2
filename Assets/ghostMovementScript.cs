using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using KModkit;

public class ghostMovementScript : MonoBehaviour {

    public KMBombInfo Bomb;
    public KMAudio Audio;

    //Logging
    static int moduleIdCounter = 1;
    int moduleId;
    private bool moduleSolved;

    public KMSelectable[] GhostButtons; //Blinky, Pinky, Inky, Clyde
    public KMSelectable PacManButton;
    public TextMesh[] Screens;
    public Color[] TextColors; //white, green, red
    public Material Killscreen; //easter egg
    public GameObject Background;

    private string maze =   "############################"+ //28 wide, 32 high
                            "#.════.═════.##.═════.════.#"+
                            "#║####║#####║##║#####║####║#"+
                            "#║####║#####║##║#####║####║#"+
                            "#║####║#####║##║#####║####║#"+
                            "#.════.══.════════.══.════.#"+
                            "#║####║##║########║##║####║#"+
                            "#║####║##║########║##║####║#"+
                            "#.════.##.══.##.══.##.════.#"+
                            "######║#####║##║#####║######"+
                            "######║#####║##║#####║######"+
                            "######║##.══X══X══.##║######"+ //Xs at 320, 323, 656, 659
                            "######║##║########║##║######"+
                            "######║##║########║##║######"+
                            "══════.══.########.══.══════"+ //the ends of this line are at 392, 419
                            "######║##║########║##║######"+
                            "######║##║########║##║######"+
                            "######║##.════════.##║######"+
                            "######║##║########║##║######"+
                            "######║##║########║##║######"+
                            "#.════.══.══.##.══.══.════.#"+
                            "#║####║#####║##║#####║####║#"+
                            "#║####║#####║##║#####║####║#"+
                            "#.═.##.══.══X══X══.══.##.═.#"+
                            "###║##║##║########║##║##║###"+
                            "###║##║##║########║##║##║###"+
                            "#.═.══.##.══.##.══.##.══.═.#"+
                            "#║##########║##║##########║#"+
                            "#║##########║##║##########║#"+
                            "#.══════════.══.══════════.#"+
                            "############################"+
                            "############################"; //C is at 868, bottom left
    private int pacmanPos, blinkyPos, pinkyPos, inkyPos, clydePos;
    private int pacmanDir, blinkyDir, pinkyDir, inkyDir, clydeDir; //ULDR
    private string arrows = "↑←↓→";
    private int blinkyAns, pinkyAns, inkyAns, clydeAns;
    private int blinkyCho, pinkyCho, inkyCho, clydeCho;
    private int pointX;
    private int pacX, pacY;
    private int pinkyT, inkyT;
    private int inkyX, inkyY;
    private int clydeX, clydeY;
    private bool clydeIsEightAway = false;
    private int correctDirections = 0;

    private Coroutine buttonHold;
    private bool holding = false;

    void Awake () {
        moduleId = moduleIdCounter++;

        foreach (KMSelectable gButton in GhostButtons) {
            gButton.OnInteract += delegate () { GhostPress(gButton); return false; };
        }

        PacManButton.OnInteract += delegate () { PacManPress(); return false; };
        PacManButton.OnInteractEnded += delegate { PacManRelease(); };
    }

    // Use this for initialization
    void Start () {
        pacmanPos = GetPlace();
        blinkyPos = GetPlace();
        pinkyPos = GetPlace();
        inkyPos = GetPlace();
        clydePos = GetPlace();

        pacmanDir = GetDirection(pacmanPos);
        blinkyDir = GetDirection(blinkyPos);
        pinkyDir = GetDirection(pinkyPos);
        inkyDir = GetDirection(inkyPos);
        clydeDir = GetDirection(clydePos);

        Debug.LogFormat("[Ghost Movement #{0}] Pac-Man: {1} {2}", moduleId, LocationName(pacmanPos), arrows[pacmanDir]);
        Debug.LogFormat("[Ghost Movement #{0}] Blinky: {1} {2}", moduleId, LocationName(blinkyPos), arrows[blinkyDir]);
        Debug.LogFormat("[Ghost Movement #{0}] Pinky: {1} {2}", moduleId, LocationName(pinkyPos), arrows[pinkyDir]);
        Debug.LogFormat("[Ghost Movement #{0}] Inky: {1} {2}", moduleId, LocationName(inkyPos), arrows[inkyDir]);
        Debug.LogFormat("[Ghost Movement #{0}] Clyde: {1} {2}", moduleId, LocationName(clydePos), arrows[clydeDir]);

        Screens[0].text = LocationName(pacmanPos) + "\n\n" + arrows[pacmanDir];
        Screens[1].text = LocationName(blinkyPos) + "\n\n" + arrows[blinkyDir];
        Screens[2].text = LocationName(pinkyPos) + "\n\n" + arrows[pinkyDir];
        Screens[3].text = LocationName(inkyPos) + "\n\n" + arrows[inkyDir];
        Screens[4].text = LocationName(clydePos) + "\n\n" + arrows[clydeDir];

        blinkyCho = blinkyDir;
        pinkyCho = pinkyDir;
        inkyCho = inkyDir;
        clydeCho = clydeDir;

        blinkyAns = Target(blinkyPos, blinkyDir, pacmanPos);

        pacX = pacmanPos%28; pacY = pacmanPos/28;
        switch (pacmanDir) {
            case 0: pinkyT = (Clamp('Y', pacY-4)*28 + Clamp('X', pacX-4));  pointX = (Clamp('Y', pacY-2)*28 + Clamp('X', pacX-2));  break;
            case 1: pinkyT = (pacY*28 + Clamp('X', pacX-4));                pointX = (pacY*28 + Clamp('X', pacX-2));                break;
            case 2: pinkyT = (Clamp('Y', pacY+4)*28 + pacX);                pointX = (Clamp('Y', pacY+2)*28 + pacX);                break;
            case 3: pinkyT = (pacY*28 + Clamp('X', pacX+4));                pointX = (pacY*28 + Clamp('X', pacX+2));                break;
        }
        Debug.LogFormat("[Ghost Movement #{0}] Pinky's Target: {1}", moduleId, LocationName(pinkyT));
        Debug.LogFormat("[Ghost Movement #{0}] Point X: {1}", moduleId, LocationName(pointX));
        pinkyAns = Target(pinkyPos, pinkyDir, pinkyT);

        inkyX = pointX%28 - blinkyPos%28;
        inkyY = pointX/28 - blinkyPos/28;
        inkyT = Clamp('Y', inkyY + pacY)*28 + Clamp('X', inkyX + pacX);
        Debug.LogFormat("[Ghost Movement #{0}] Inky's Target: {1}", moduleId, LocationName(inkyT));
        inkyAns = Target(inkyPos, inkyDir, inkyT);

        clydeX = Math.Abs(clydePos%28 - pacX);
        clydeY = Math.Abs(clydePos/28 - pacY);
        switch (clydeX) {
            case 0: case 1: case 2: if (clydeY <= 8) { clydeIsEightAway = true; } break;
            case 3: case 4: if (clydeY <= 7) { clydeIsEightAway = true; } break;
            case 5: case 6: if (clydeY <= 6) { clydeIsEightAway = true; } break;
            case 7: if (clydeY <= 4) { clydeIsEightAway = true; } break;
            case 8: if (clydeY <= 2) { clydeIsEightAway = true; } break;
            default: break;
        }
        if (clydeIsEightAway) {
            Debug.LogFormat("[Ghost Movement #{0}] Clyde is within the 8-spaces radius, targetting 4531.", moduleId);
            clydeAns = Target(clydePos, clydeDir, 868);
        } else {
            Debug.LogFormat("[Ghost Movement #{0}] Clyde is not within the 8-spaces radius, targetting Pac-Man.", moduleId);
            clydeAns = Target(clydePos, clydeDir, pacmanPos);
        }

        Debug.LogFormat("[Ghost Movement #{0}] Answers = Blinky:{1} Pinky:{2} Inky:{3} Clyde:{4}", moduleId, arrows[blinkyAns], arrows[pinkyAns], arrows[inkyAns], arrows[clydeAns]);

        if (blinkyAns != blinkyDir && pinkyAns != pinkyDir && inkyAns != inkyDir && clydeAns != clydeDir) {
            Background.GetComponent<MeshRenderer>().material = Killscreen;
        }
    }

    int GetPlace () {
        int o = 0;

        while (maze[o].ToString() == "#".ToString() || o == pacmanPos) {
            o = UnityEngine.Random.Range(0, 896);
        }

        return o;
    }

    int GetDirection (int p) {
        int RNG = 0;

        switch (maze[p].ToString()) {
            case ".":
                RNG = UnityEngine.Random.Range(0,4);
            break;
            case "║":
                RNG = UnityEngine.Random.Range(0,2);
                if (RNG == 1) {RNG = 2;}
            break;
            case "═":
                RNG = UnityEngine.Random.Range(0,2);
                if (RNG == 0) {RNG = 3;}
            break;
            case "X":
                RNG = UnityEngine.Random.Range(0,3);
                RNG += 1;
            break;
        }

        return RNG;
    }

    int Clamp (char d, int n) {
        switch (d) {
            case 'X':
            if (n < 0) {
                return 0;
            } else if (n > 27) {
                return 27;
            }
            break;
            case 'Y':
            if (n < 0) {
                return 0;
            } else if (n > 30) {
                return 30;
            }
            break;
        }
        return n;
    }

    int Target (int gho, int dir, int tar) {
        bool u = true; bool l = true; bool d = true; bool r = true;

        if (maze[gho].ToString() == "X".ToString()) { u = false; } //Rule out up if you're at an X

        switch (dir) { //Rule out direction from behind
            case 0: d = false; break;
            case 1: r = false; break;
            case 2: u = false; break;
            case 3: l = false; break;
        }

        if (gho == 392 || gho == 419) { u = false; d = false; } else { //Rule out directions with walls
        if (maze[gho-28].ToString() == "#".ToString()) { u = false; }
        if (maze[gho-1].ToString() == "#".ToString()) { l = false; }
        if (maze[gho+28].ToString() == "#".ToString()) { d = false; }
        if (maze[gho+1].ToString() == "#".ToString()) { r = false; }
        }

        int good = 0;
        int onlyOne = -1;
        if (u) { good +=1; onlyOne = 0;};
        if (l) { good +=1; onlyOne = 1;};
        if (d) { good +=1; onlyOne = 2;};
        if (r) { good +=1; onlyOne = 3;};

        if (good == 1) { //If we only have one direction up to this point, use it.
            return onlyOne;
        } else { //If not...
            int tarX = tar%28; int tarY = tar/28;
            int ghoX = gho%28; int ghoY = gho/28;

            List<int> possible = new List<int> {};
            if (u) {possible.Add(0);} if (l) {possible.Add(1);} if (d) {possible.Add(2);} if (r) {possible.Add(3);}
            List<int> distances = new List<int> {};

            for (int k = 0; k < possible.Count(); k++) { //find the distances of all the remaining directions
                switch (possible[k]) {
                    case 0: ghoY -= 1; break;
                    case 1: ghoX -= 1; break;
                    case 2: ghoY += 1; break;
                    case 3: ghoX += 1; break;
                }
                distances.Add(Math.Abs(ghoX - tarX)*Math.Abs(ghoX - tarX) + Math.Abs(ghoY - tarY)*Math.Abs(ghoY - tarY));
                ghoX = gho%28; ghoY = gho/28;
            }

            if (good == 2) { //and use the one which minimises distance.
                if (distances[0] < distances[1]) {
                    return possible[0];
                } else if (distances[1] < distances[0]) {
                    return possible[1];
                }
            } else if (good == 3) {
                if (distances[0] < distances[1] && distances[0] < distances[2]) {
                    return possible[0];
                } else if (distances[1] < distances[0] && distances[1] < distances[2]) {
                    return possible[1];
                } else if (distances[2] < distances[0] && distances[2] < distances[1]) {
                    return possible[2];
                }
                if (distances[0] == distances[1]) {
                    possible.RemoveAt(2);
                } else if (distances[0] == distances[2]) {
                    possible.RemoveAt(1);
                } else if (distances[1] == distances[2]) {
                    possible.RemoveAt(0);
                }
            }

            Debug.Log(possible[0]);
            return possible[0]; //if there's a tie, just use highest priority
        }
    }

    string LocationName (int x) {
        List<string> colLet = new List<string> {"45", "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M", "N", "O", "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z", "a"};
        List<string> rowNum = new List<string> {"0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "13", "14", "15", "16", "17", "18", "19", "20", "21", "22", "23", "24", "25", "26", "27", "28", "29", "30", "31"};

        int r = x/28;
        int c = x%28;

        return colLet[c] + "" + rowNum[r];
    }

    void GhostPress(KMSelectable Button) {
        Button.AddInteractionPunch();
        if (!moduleSolved) {
            Audio.PlaySoundAtTransform("GM press", transform);
            if (Button == GhostButtons[0]) { //Blinky
                blinkyCho = (blinkyCho + 1) % 4;
                Screens[1].text = LocationName(blinkyPos) + "\n\n" + arrows[blinkyCho];
            } else if (Button == GhostButtons[1]) { //Pinky
                pinkyCho = (pinkyCho + 1) % 4;
                Screens[2].text = LocationName(pinkyPos) + "\n\n" + arrows[pinkyCho];
            } else if (Button == GhostButtons[2]) { //Inky
                inkyCho = (inkyCho + 1) % 4;
                Screens[3].text = LocationName(inkyPos) + "\n\n" + arrows[inkyCho];
            } else if (Button == GhostButtons[3]) { //Clyde
                clydeCho = (clydeCho + 1) % 4;
                Screens[4].text = LocationName(clydePos) + "\n\n" + arrows[clydeCho];
            }
            Screens[1].color = TextColors[0];
            Screens[2].color = TextColors[0];
            Screens[3].color = TextColors[0];
            Screens[4].color = TextColors[0];
        }
    }

    void PacManPress () {
        PacManButton.AddInteractionPunch();
        if (!moduleSolved) {
            if (buttonHold != null)
    		{
    			holding = false;
    			StopCoroutine(buttonHold);
    			buttonHold = null;
    		}

    		buttonHold = StartCoroutine(HoldChecker());
        }
    }

    void PacManRelease () {
        StopCoroutine(buttonHold);
        if (holding) {
            Screens[1].text = LocationName(blinkyPos) + "\n\n" + arrows[blinkyDir];
            Screens[2].text = LocationName(pinkyPos) + "\n\n" + arrows[pinkyDir];
            Screens[3].text = LocationName(inkyPos) + "\n\n" + arrows[inkyDir];
            Screens[4].text = LocationName(clydePos) + "\n\n" + arrows[clydeDir];
            blinkyCho = blinkyDir;
            pinkyCho = pinkyDir;
            inkyCho = inkyDir;
            clydeCho = clydeDir;
            Audio.PlaySoundAtTransform("GM press", transform);
        } else {
            correctDirections = 0;
            if (blinkyAns == blinkyCho) { correctDirections += 1; Screens[1].color = TextColors[1]; } else { Screens[1].color = TextColors[2]; }
            if (pinkyAns == pinkyCho) { correctDirections += 1; Screens[2].color = TextColors[1]; } else { Screens[2].color = TextColors[2]; }
            if (inkyAns == inkyCho) { correctDirections += 1; Screens[3].color = TextColors[1]; } else { Screens[3].color = TextColors[2]; }
            if (clydeAns == clydeCho) { correctDirections += 1; Screens[4].color = TextColors[1]; } else { Screens[4].color = TextColors[2]; }
            if (correctDirections == 4) {
                Debug.LogFormat("[Ghost Movement #{0}] Submitted = Blinky:{1} Pinky:{2} Inky:{3} Clyde:{4}; This is correct, module solved.", moduleId, arrows[blinkyAns], arrows[pinkyAns], arrows[inkyAns], arrows[clydeAns]);
                Audio.PlaySoundAtTransform("GM solve", transform);
                GetComponent<KMBombModule>().HandlePass();
                moduleSolved = true;
            } else {
                Debug.LogFormat("[Ghost Movement #{0}] Submitted = Blinky:{1} Pinky:{2} Inky:{3} Clyde:{4}; Incorrect. Strike!", moduleId, arrows[blinkyCho], arrows[pinkyCho], arrows[inkyCho], arrows[clydeCho]);
                Audio.PlaySoundAtTransform("GM strike", transform);
                GetComponent<KMBombModule>().HandleStrike();
            }
        }
        holding = false;
    }

    IEnumerator HoldChecker()
    {
    	yield return new WaitForSeconds(0.4f);
    	holding = true;
    }
#pragma warning disable 414
    private const string TwitchHelpMessage = "Reset the module using !{0} reset. Submit an answer using !{0} submit u l d r. Reading order.";
#pragma warning restore 414
    private IEnumerator ProcessTwitchCommand(string command)
    {
        command = command.ToLowerInvariant().Trim();
        if (command == "reset")
        {
            yield return null;
            PacManButton.OnInteract();
            yield return new WaitForSeconds(.7f);
            PacManButton.OnInteractEnded();
            yield break;
        }

        if (command.StartsWith("submit"))
        {
            var parsedCommand = command.Split(' ');
            if (parsedCommand.Length != 5)
            {
                yield return "sendtochaterror Invalid length of command!";
                yield break;
            }

            var instructions = parsedCommand.TakeLast(4).ToArray();
            if (!instructions.All(x => new[]{"u","d","l","r"}.Contains(x)))
            {
                yield return "sendtochaterror There is an invalid character in the command, you can only use U,D,L,R!";
                yield break;
            }

            var selectables = new List<KMSelectable>();
            for (var i = 0; i < 4; ++i)
            {
                var instructionIndex = Array.IndexOf(new[]{"u","l","d","r"}, instructions[i]);
                if (GetCurrentDir(i) == instructionIndex)
                {
                    continue;
                }
                selectables.AddRange(Enumerable.Repeat(GhostButtons[i], Mod(instructionIndex - GetCurrentDir(i))));
            }

            yield return null;
            foreach (var selectable in selectables)
            {
                selectable.OnInteract();
                yield return new WaitForSeconds(.1f);
            }
            PacManButton.OnInteract();
            yield return new WaitForSeconds(.01f);
            PacManButton.OnInteractEnded();
        }
    }

    private IEnumerator TwitchHandleForcedSolve()
    {
        for (var i = 0; i < 4; i++)
        {
            while (GetCurrentDir(i) != GetCurrentAns(i))
            {
                GhostButtons[i].OnInteract();
                yield return new WaitForSeconds(.1f);
            }
        }
        PacManButton.OnInteract();
        yield return new WaitForSeconds(.01f);
        PacManButton.OnInteractEnded();
    }

    private int GetCurrentDir(int i)
    {
        switch (i)
        {
            case 0:
                return blinkyCho;
            case 1:
                return pinkyCho;
            case 2:
                return inkyCho;
            case 3:
                return clydeCho;
            default:
                throw new InvalidOperationException(string.Format("Error in GetCurrentDir: {0}", i));
        }
    }
    
    private int GetCurrentAns(int i)
    {
        switch (i)
        {
            case 0:
                return blinkyAns;
            case 1:
                return pinkyAns;
            case 2:
                return inkyAns;
            case 3:
                return clydeAns;
            default:
                throw new InvalidOperationException(string.Format("Error in GetCurrentAns: {0}", i));
        }
    }

    private static int Mod(int i)
    {
        return i < 0 ? i + 4 : i;
    }
}
