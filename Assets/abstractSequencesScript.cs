using System.Collections;
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using KModkit;

public class abstractSequencesScript : MonoBehaviour {

ArrayList numbers = new ArrayList();
ArrayList finalSequence = new ArrayList(20);
public KMAudio Audio;
public KMBombInfo BombInfo;
public KMSelectable[] dataButtons;
public Renderer[] leds;
public Material[] ledStates;
public TextMesh[] texts;
private bool generated;
private int[] contains = new int[2];
private int strikeCount = 0;
private int solvedModules = 0;
public TextMesh displayText;
public Color[] textColors;
private String[] operation = new String[1];
private String result = "";
private String resultB = "";
private String serialNo = "";
private double[][] statements = new double[6][];
private string[][] operations = new string[][]
{
	new string[] {"+", "+", "*", "/"},
	new string[] {"/", "+", "-", "*"},
	new string[] {"-", "*", "/", "-"},
	new string[] {"*", "/", "-", "+"},
};
private string[][] textVersions = new string[][]
{
	new string[] { "(x+7)", "(z+n)", "(z(n-1))", "(x^2)", "(y^2)" },
	new string[] { "(8y)", "(z-y)", "(2z)", "(4n)", "(termOne*4)" },
	new string[] { "(termOne*n)", "(x/3)", "(z^2)", "(n^2)", "(5z)" },
	new string[] { "(5n)", "(n-3)", "(termOne)", "(x+2y)", "(x+4)" },
	new string[] { "(n+2)", "(n/4)", "(-n+5)", "(5z)", "(x(2+n))" },
	new string[] { "(6z-n)", "(x+y+n)", "(y+2z)", "(z+5n)", "(yz+2n)" },
};

public bool[] buttonStates;
private int[] buttonNumbers = new int[17];
private double[] importantValues = new double[9];

int PortParallel;
int PortSerial;
int PortDVI;
int PortStereoRCA;
int PortRJ45;
int PortPS2;
/*
	importantValues[0] = x
	importantValues[1] = y
	importantValues[2] = z
	importantValues[3] = n
	importantValues[4] = Term 1
	importantValues[5] = numeratorCol;
	importantValues[6] = numeratorRow;
	importantValues[7] = denominatorCol;
	importantValues[8] = denominatorRow;
*/
private bool canClickAgain = false;
private float increment = 0f;

static int moduleIDCounter = 1;
int ModuleID;
private bool moduleSolved;
public string TwitchHelpMessage = "Press the button in a certain position in reading order using !{0} press #, submit using !{0} press submit.";


void Awake () {

	serialNo = BombInfo.GetSerialNumber();
	PortParallel = BombInfo.GetPortCount(Port.Parallel);
  PortSerial = BombInfo.GetPortCount(Port.Serial);
  PortDVI = BombInfo.GetPortCount(Port.DVI);
  PortStereoRCA = BombInfo.GetPortCount(Port.StereoRCA);
  PortRJ45 = BombInfo.GetPortCount(Port.RJ45);
  PortPS2 = BombInfo.GetPortCount(Port.PS2);
	Debug.Log("Serial number: " + serialNo);
	GetComponent<KMBombModule>().OnActivate += Activate;
	foreach (KMSelectable button in dataButtons)
	{
			button.OnInteract += delegate () { ActionPress(button); return false; };
	}
}
	// Use this for initialization
	void Start () {
		ModuleID = moduleIDCounter++;
	}

	// Update is called once per frame
	void Update () {
		if (!moduleSolved){
			int tempA = BombInfo.GetStrikes();
			int tempB = BombInfo.GetSolvedModuleNames().Count;
			if (tempA != strikeCount){
				generated = false;
				Debug.LogFormat("[Abstract Sequences #{0}] Strike detected! Updating module solution for {1} strike(s)...", ModuleID, tempA);
				GenerateVariables();
				strikeCount = tempA;
			}
			if (tempB != solvedModules){
				generated = false;
				Debug.LogFormat("[Abstract Sequences #{0}] Solve detected! Updating module solution for {1} solve(s)...", ModuleID, tempB);
				GenerateSequence();
				solvedModules = tempB;
			}
		}
	}

	IEnumerator FlashDisplay(){

		canClickAgain = false;
		displayText.color = textColors[1];
		increment = 0.1f;
		while (increment > 0f){
			yield return new WaitForSeconds(increment / 2);
			displayText.color = textColors[0];
			yield return new WaitForSeconds(increment / 2);
			displayText.color = textColors[1];
			increment -= 0.01f;
		}
		displayText.color = textColors[2];
		canClickAgain = true;
		StopCoroutine(FlashDisplay());
	}
	void ClearNumbers(){
		for (int i = 0; i < 16; i++){
			buttonNumbers[i] = 0;
		}
	}
	IEnumerator UnrevealNumbers(){
		foreach (TextMesh text in texts){
			text.text = "";
			yield return new WaitForSeconds(0.05f);
		}
		StopCoroutine(UnrevealNumbers());
	}
	IEnumerator RewindDisplay(){
		while (buttonNumbers[16] > 0){
			buttonNumbers[16]--;
			displayText.text = ("" + buttonNumbers[16]);
			yield return new WaitForSeconds(0.0008f);
		}
		StopCoroutine(RewindDisplay());
	}
	IEnumerator RevealNumbers(){
		canClickAgain = false;
		int timeSubmit = ((int) (BombInfo.GetTime()) / 60);
		if (timeSubmit % 2 == 0){
			Debug.LogFormat("[Abstract Sequences #{0}] Submitted answer at "+timeSubmit+" minutes. All numbers expected in ascending order.", ModuleID);
		} else {
			Debug.LogFormat("[Abstract Sequences #{0}] Submitted answer at "+timeSubmit+" minutes. All numbers expected in descending order.", ModuleID);
		}

		contains[1] = 0;
		result = "Sequence of buttons pressed: ";
		foreach (int num in numbers){
			result += (buttonNumbers[num] + " ");
			if (!finalSequence.Contains(buttonNumbers[num])){
				contains[1]++;
			}
		}
		Debug.LogFormat("[Abstract Sequences #{0}] {1}", ModuleID, result);
		result = "";
		for (int j = 0; j < 16; j++){
			texts[j].text = "";
		}
		foreach (int num in numbers){
			result += (buttonNumbers[num] + " ");
			texts[num].text = ("" + buttonNumbers[num]);
			yield return new WaitForSeconds(0.5f);
		}
		yield return new WaitForSeconds(0.5f);
		if (timeSubmit % 2 == 0){
			CheckSubmitOrder(true);
		} else {
			CheckSubmitOrder(false);
		}
		canClickAgain = true;
		StopCoroutine(RevealNumbers());
	}
	IEnumerator DisplayNumbers(){
		for (int i = 0; i < 16; i++){
			yield return new WaitForSeconds(0.05f);
			texts[i].text = ("" + buttonNumbers[i]);
		}
		StopCoroutine(DisplayNumbers());
	}
	IEnumerator FlashNumbers(){
		int i = 0;
		while (i < 4){
			for (int j = 0; j < 16; j++){
				if (numbers.Contains(j)){
					leds[j].material = ledStates[2];
				}
			}
			yield return new WaitForSeconds(0.18f);
			for (int j = 0; j < 16; j++){
				if (numbers.Contains(j)){
					leds[j].material = ledStates[0];
				}
			}
			yield return new WaitForSeconds(0.06f);
			i++;
		}
		StartCoroutine(DisplayNumbers());
		numbers.Clear();
		StopCoroutine(FlashNumbers());
	}
	void CheckSubmitOrder(bool whichWay){
		if ((InOrder(whichWay)) && (contains[0] == contains[1])){
			GetComponent<KMBombModule>().HandlePass();
			Debug.LogFormat("[Abstract Sequences #{0}] Submitted sequence is correct. Module solved.", ModuleID);
			StartCoroutine(RewindDisplay());
			StartCoroutine(UnrevealNumbers());
			moduleSolved = true;
		} else {
			Debug.LogFormat("[Abstract Sequences #{0}] Submitted sequence is incorrect. Module striked.", ModuleID);
			GetComponent<KMBombModule>().HandleStrike();
			StartCoroutine(FlashNumbers());
			StartCoroutine(FlashDisplay());
		}
	}
	void Activate(){
		GenerateDisplay();
		StartCoroutine(FlashDisplay());
		ClearNumbers();
		GenerateNumbers();
		StartCoroutine(DisplayNumbers());
		GenerateVariables();
	}
	void ActionPress(KMSelectable buttonPressed){
		if (canClickAgain && !moduleSolved)
		{
			String a = (buttonPressed.name).ToLower();
			for (int i = 0; i < 16; i++){
				if (buttonPressed.name.Equals(dataButtons[i].name)){
					if (!numbers.Contains(i)){
						GetComponent<KMAudio>().PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
						buttonPressed.AddInteractionPunch();
						ToggleState(i);
					}
				}
			}
			if (a == "submit"){
				GetComponent<KMAudio>().PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
				buttonPressed.AddInteractionPunch();
				TurnAllOff();
				StartCoroutine(RevealNumbers());
			}
		}
	}
	void ToggleState(int buttonPos){
			for (int i = 0; i < 16; i++)
			{
				texts[i].text = ("?");
			}
			buttonStates[buttonPos] = true;
			leds[buttonPos].material = ledStates[1];
			if (!numbers.Contains(buttonPos)) {
			numbers.Add(buttonPos);
			}
	}
	bool InOrder(bool ascending){
		int low = 0;
		int high = 0;
		if (ascending){
			low = 0;
			foreach (int num in numbers){
				if (buttonNumbers[num] > low){
					low = buttonNumbers[num];
				} else {
					return false;
				}
			}
			return true;
		} else {
			high = 100;
			foreach (int num in numbers){
				if (buttonNumbers[num] < low){
					low = buttonNumbers[num];
				} else {
					return false;
				}
			}
			return true;
		}
	}
	void TurnAllOff(){
		for (int i = 0; i < 16; i++){
			buttonStates[i] = false;
			leds[i].material = ledStates[0];
		}
	}
	void GenerateDisplay(){
		displayText.text = "";
		buttonNumbers[16] = UnityEngine.Random.Range(0,100);
		Debug.LogFormat("[Abstract Sequences #{0}] Displayed number: "+ buttonNumbers[16], ModuleID);
		if (buttonNumbers[16] < 10){
			displayText.text += ("" + 0);
		}
		displayText.text += ("" + buttonNumbers[16]);
	}
	void GenerateVariables(){
		for (int i = 0; i < 3; i++){
			SetValue(i, 0);
		}
		int sumFirstColumn = 0;
		int letters = 0;
		int positionLetter = 0;
		if (isPrime(buttonNumbers[16])){
			EditValues(3,39,27,0,0);
		} else {
			EditValues(21,48,15,0,0);
		}
		for (int i = 0; i < 4; i++){
			sumFirstColumn += buttonNumbers[i*4];
		}
		if (Math.Abs(sumFirstColumn) % 2 == 0){
			EditValues(30,51,36,0,0);
		} else {
			EditValues(33,9,45,0,0);
		}
		if (BombInfo.GetSerialNumberNumbers().Last() < 5){
			EditValues(42,24,6,0,0);
		} else {
			EditValues(12,18,0,0,0);
		}
		for (int i = 0; i < 3; i++){
			importantValues[i] /= 3;
		}
		if (PortStereoRCA > 0 || PortDVI > 0 || PortParallel > 0){
			EditValues(-2,3,5,0,0);
		}
		if (PortPS2 > 0 || PortSerial > 0 || PortRJ45 > 0){
			EditValues(-6,-1,-3,0,0);
		}
		if (IndicatorPresent("IND") || IndicatorPresent("BOB") || IndicatorPresent("MSA")){
			EditValues(6,-5,2,0,0);
		}
		if (IndicatorPresent("SND") || IndicatorPresent("CAR") || IndicatorPresent("NSA")){
			EditValues(1,-3,-4,0,0);
		}
		Debug.LogFormat("[Abstract Sequences #{0}] Final values of x, y, z: {1}, {2}, {3}", ModuleID, importantValues[0], importantValues[1], importantValues[2]);
		importantValues[4] = SmallestVariable(importantValues[0], importantValues[1], importantValues[2]);
		Debug.LogFormat("[Abstract Sequences #{0}] Term one of sequence: {1}", ModuleID, importantValues[4]);
		GenerateEquations();
	}
	void GenerateEquations(){
		if (!generated){
		int test = BombInfo.GetBatteryHolderCount();
		if (test < 2){
			importantValues[5] = 0;
		} else if (test < 4){
			importantValues[5] = 1;
		} else {
			importantValues[5] = 2;
		}

		test = buttonNumbers[16];
		if (test < 20){
			importantValues[6] = 0;
		} else if (test < 40){
			importantValues[6] = 1;
		} else if (test < 60){
			importantValues[6] = 2;
		} else if (test < 80){
			importantValues[6] = 3;
		} else {
			importantValues[6] = 4;
		}

		test = (BombInfo.GetPortCount() + BombInfo.GetPortPlateCount());
		if (test < 5){
			importantValues[7] = 3;
		} else if (test < 9){
			importantValues[7] = 4;
		} else {
			importantValues[7] = 5;
		}

		test = BombInfo.GetStrikes();
		if (test < 2){
			importantValues[8] = 0;
		} else if (test < 4){
			importantValues[8] = 1;
		} else if (test < 6){
			importantValues[8] = 2;
		} else if (test < 8){
			importantValues[8] = 3;
		} else {
			importantValues[8] = 4;
		}
		generated = true;
		GenerateOperation();
		}
	}
	void GenerateOperation(){
		int first = 0;
		int condCount = 0;
		int second = 0;
		if (BombInfo.GetBatteryHolderCount() > 3){
			first+= 2;
			condCount++;
		}
		if (BombInfo.GetOffIndicators().Count() > 1){
			first+= 4;
			condCount++;
		}
		if (buttonNumbers[16] % 2 != 0){
			first+= 8;
			condCount++;
		}
		if (condCount == 0 || condCount > 1){
			first = 3;
		} else {
			first = ((int)(Math.Log(first, 2)))-1;
		}
		condCount = 0;
		if (BombInfo.GetPorts().Distinct().Count() > 1){
			second += 2;
			condCount++;
		}
		if (BombInfo.GetSerialNumberNumbers().Sum() > 14){
			second+= 4;
			condCount++;
		}
		if (BombInfo.GetModuleNames().Count() > 10){
			second+= 8;
			condCount++;
		}
		if (condCount == 0 || condCount > 1){
			second = 3;
		} else {
			second = ((int)(Math.Log(second, 2)))-1;
		}
		operation[0] = operations[first][second];
		Debug.LogFormat("[Abstract Sequences #{0}] Equation chosen: "+ textVersions[(int)importantValues[5]][(int)importantValues[6]] + " " + operation[0] + " " + textVersions[(int)importantValues[7]][(int)importantValues[8]], ModuleID);
		GenerateSequence();
	}
	void GenerateSequence(){
		contains[0] = 0;
		String sequence = "";

		String sequenceB = "Final sequence (with modulo & absolute value): ";
		int a = 0;
		int b = 1;
		finalSequence.Clear();
		importantValues[3] = 1;
		if (!(UnicornAvailable())){
			sequence = "Calculated terms: " + (int)importantValues[4] + " ";
			finalSequence.Add((int) importantValues[4]);
			for (int i = 0; i < 19; i++)
			{
				UpdateValues();
				if (operation[0] == "+"){
					sequence += (((int)((statements[(int)importantValues[5]][(int)importantValues[6]]) + (statements[(int)importantValues[7]][(int)importantValues[8]]))) + " ");
					finalSequence.Add(Math.Abs((int)(((statements[(int)importantValues[5]][(int)importantValues[6]]) + (statements[(int)importantValues[7]][(int)importantValues[8]]))%100)));
				} else if (operation[0] == "-"){
					sequence += (((int)((statements[(int)importantValues[5]][(int)importantValues[6]]) - (statements[(int)importantValues[7]][(int)importantValues[8]]))) + " ");
					finalSequence.Add(Math.Abs((int)(((statements[(int)importantValues[5]][(int)importantValues[6]]) - (statements[(int)importantValues[7]][(int)importantValues[8]]))%100)));
				} else if (operation[0] == "*"){
					sequence += (((int)((statements[(int)importantValues[5]][(int)importantValues[6]]) * (statements[(int)importantValues[7]][(int)importantValues[8]]))) + " ");
					finalSequence.Add(Math.Abs((int)(((statements[(int)importantValues[5]][(int)importantValues[6]]) * (statements[(int)importantValues[7]][(int)importantValues[8]]))%100)));
				} else if ((statements[(int)importantValues[7]][(int)importantValues[8]]) != 0){
					sequence += (((int)((statements[(int)importantValues[5]][(int)importantValues[6]]) / (statements[(int)importantValues[7]][(int)importantValues[8]]))) + " ");
					finalSequence.Add(Math.Abs((int)(((statements[(int)importantValues[5]][(int)importantValues[6]]) / (statements[(int)importantValues[7]][(int)importantValues[8]]))%100)));
				}
			}
		} else {
			Debug.LogFormat("[Abstract Sequences #{0}] Unicorn rule active! Overriding generated terms with Fibonacci sequence...", ModuleID);
			sequence = "Final sequence: ";
			finalSequence.Add(b);
			sequence += (b + " ");
			for (int i = 0; i < 19; i++){
				if (i % 2 == 0){
					a += b;
					finalSequence.Add(a%100);
					sequence += (a + " ");
				} else {
					b += a;
					finalSequence.Add(b%100);
					sequence += (b + " ");
				}
			}
		}
		foreach (int num in finalSequence){
			sequenceB += (num + " ");
		}
		String expectation = "Numbers expected in the submitted answer: ";
		Debug.LogFormat("[Abstract Sequences #{0}] " + sequence, ModuleID);
		Debug.LogFormat("[Abstract Sequences #{0}] " + sequenceB, ModuleID);
		sequence = "";
		sequenceB = "";
		bool comma = false;
		for (int i = 0; i < 16; i++){
			if (!(finalSequence.Contains(buttonNumbers[i]))){
				if (comma){
					expectation += ", ";
				} else {
					comma = true;
				}
				expectation += ("" + buttonNumbers[i]);
				contains[0]++;
			}
		}
		Debug.LogFormat("[Abstract Sequences #{0}] " + expectation + ".", ModuleID);
	}
	bool IndicatorPresent(string ind){
		if (BombInfo.IsIndicatorOn(ind) || BombInfo.IsIndicatorOff(ind)){
			return true;
		}
		return false;
	}
	void GenerateNumbers(){
		result = "";
		for (int i = 0; i < 16; i++){
			int temp = UnityEngine.Random.Range(1,100);
			while(buttonNumbers.Contains(temp)){
				temp = UnityEngine.Random.Range(1,100);
			}
			result += (temp + " ");
			buttonNumbers[i] = temp;
		}
		Debug.LogFormat("[Abstract Sequences #{0}] Initial numbers, in reading order: " + result, ModuleID);
		StartCoroutine(DisplayNumbers());
	}
	void EditValues(int x, int y, int z, int n, int termOne){
		importantValues[0] += x;
		importantValues[1] += y;
		importantValues[2] += z;
		importantValues[3] += n;
		importantValues[4] += termOne;
	}
	double SmallestVariable(double x, double y, double z){
		if (x <= y && x <= z){
			return x;
		} else if (y <= z){
			return y;
		} else {
			return z;
		}
	}
	bool UnicornAvailable(){
		int condCount = 0;
		if (isPrime(BombInfo.GetBatteryCount() + BombInfo.GetBatteryHolderCount())){
			condCount++;
		}
		if (isPrime(digitalRoot(buttonNumbers[16]))){
			condCount++;
		}
		if (isPrime(BombInfo.GetSolvedModuleNames().Count)){
			condCount++;
		}
		if (isPrime(buttonNumbers[0] + buttonNumbers[3] + buttonNumbers[12] + buttonNumbers[15])){
			condCount++;
		}
		if (condCount == 3){
			return true;
		}
		return false;
	}
	void SetValue(int pos, int val){
		importantValues[pos] = val;
	}
	void UpdateValues(){
		importantValues[3]++;
		UpdateValues(importantValues[0], importantValues[1], importantValues[2], importantValues[3], importantValues[4]);
	}
	void UpdateValues(double x, double y, double z, double n, double termOne){
			statements[0] = new double[5] { (x+7), (z+n), (z*(n-1)), (x*x), (y*y) };
			statements[1] = new double[5] { (8*y), (z-y), (2*z), (4*n), (termOne*4) };
			statements[2] = new double[5] { (termOne*n), (x/3), (z*z), (n*n), (5*z) };
			statements[3] = new double[5] { (n*5), (n-3), (termOne), (x+(2*y)), (x+4) };
			statements[4] = new double[5] { (n+2), (n/4), (-n+5), (5*z), (x*(2+n)) };
			statements[5] = new double[5] { ((6*z)-n), (x+y+n), (y+(2*z)), (z+(5*n)), ((y*z) + (2*n)) };
	}
	int digitalRoot(int num){
		int sum = 0;
		int length = (num.ToString()).Length;
		for (int i = length; i > 0; i--){
			sum += num % 10;
			num /= 10;
		}
		if (sum < 10){
			return sum;
		}
		return digitalRoot(sum);
	}
	bool isPrime(int num)
	{
		if (num < 2)
		{
			return false;
		}
		else
		{
			for (int i = 2; i < num; i++)
				{
					if (num % i == 0)
					{
						return false;
					}
				}
			return true;
		}
	}
}
