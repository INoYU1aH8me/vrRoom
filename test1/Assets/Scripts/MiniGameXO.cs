using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;
using NoughtsAndCrosses;
using System;

public class MiniGameXO : MonoBehaviour
{
    // time intervals measured in ticks
    private const int ticks_one_sec = 10000000;
    private const int ticks_100_ms = 1000000;
    private const int AI_player_time_limit = ticks_one_sec;


    // classic game 3x3, line of 3, no final move of noughts
    public int SizeX = 3;
    public int SizeY = 3;
    public int WinLineSize = 3;
    public bool FinalMoveOfNoughts = false;
    public int Algorithm = 1;

    const float CellInterval = 6;

    public GameObject canvasBoard;
    public GameObject statusText;
    public GameObject cellPrefab;

    private GameObject[,] cells;

    private IPlayer playerAI;
    private GameBoard game;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void InitGame()
    {
        if (canvasBoard != null && cellPrefab != null)
        {
            game = new GameBoard(SizeX, SizeY, WinLineSize, FinalMoveOfNoughts);
            playerAI = CreateAIPlayer();

            if (cells != null)
                foreach (GameObject cell in cells)
                    if (cell != null)
                        cell.transform.SetParent(null);

            cells = new GameObject[SizeX, SizeY];

            float centerX = (SizeX - 1) / 2f;
            float centerY = (SizeY - 1) / 2f;

            /*
            GameObject bg_img = canvasBoard.transform.Find("BackgroundImage").gameObject;
            Image img = (Image)bg_img.GetComponent(typeof(Image));
            img.color = Color.green;
            */

            for (int x = 0; x < SizeX; x++)
                for (int y = 0; y < SizeY; y++)
                {
                    GameObject cell = Instantiate(cellPrefab, new Vector3((x - centerX) * CellInterval, (y - centerY) * CellInterval, 0), Quaternion.identity);
                    cell.transform.SetParent(canvasBoard.transform, false);
                    Button button = (Button)cell.GetComponent(typeof(Button));

                    // creating a separate context with local copy of x,y to avoid referencing original x,y in the lambda below
                    {
                        int xx = x;
                        int yy = y;
                        button.onClick.AddListener(() => ButtonClicked(xx, yy));
                    }
                    cells[x, y] = cell;
                }
        }

    }

    void ButtonClicked(int x, int y)
    {
        if (game.NextMove != Mark.None && game.CheckField(x, y, Mark.None))
        {
            GameObject cell = cells[x, y];
            GameObject cellText = cell.transform.Find("CellText").gameObject;
            TextMeshProUGUI text = (TextMeshProUGUI)cellText.GetComponent(typeof(TextMeshProUGUI));
            text.SetText(game.NextMove == Mark.Cross ? "X" : "O");

            game.Move(x, y);

            if (game.NextMove != Mark.None)
            {
                MakeAIMove();
            }
            else
            {
                ShowWinner();
            }
        }
    }

    private void MakeAIMove()
    {
        string nextMoveMark = game.NextMove == Mark.Cross ? "X" : "O";
        playerAI.MakeMove(game);
        GameObject cell = cells[game.LatestMoveX, game.LatestMoveY];
        GameObject cellText = cell.transform.Find("CellText").gameObject;
        TextMeshProUGUI text = (TextMeshProUGUI)cellText.GetComponent(typeof(TextMeshProUGUI));
        text.SetText(nextMoveMark);

        if (game.NextMove == Mark.None)
        {
            ShowWinner();
        }
    }

    private void ShowWinner()
    {
        if (game.Winner != GameWinner.None)
        {
            ShowStatus(game.Winner == GameWinner.Draw ? "Draw!" : (game.Winner == GameWinner.Cross ? "X" : "O") + " wins!" );
            foreach(LineOfMarks line in game.WinLines)
            {
                int dx = Math.Sign(line.ToX - line.FromX);
                int dy = Math.Sign(line.ToY - line.FromY);

                for (int x = line.FromX,  y = line.FromY;  x != line.ToX + dx || y != line.ToY + dy; x += dx, y += dy)
                {
                    GameObject cell = cells[x, y];
                    Image img = (Image)cell.GetComponent(typeof(Image));
                    img.color = Color.blue;
                }
            }
        }
    }

    private void ShowStatus(string status)
    {
        if (statusText != null)
        {
            TextMeshProUGUI text = (TextMeshProUGUI)statusText.GetComponent(typeof(TextMeshProUGUI));
            text.SetText(status);
        }
    }

    private IPlayer CreateAIPlayer()
    {
        // TODO: create different types of AI players depending on settings
        if (Algorithm == 1)
        {
            return new PlayerTraverse(AI_player_time_limit);
        }
        else if (Algorithm == 2)
        {
            return new PlayerOrderedTraverse(AI_player_time_limit);
        }
        else
        {
            return new PlayerRandom();
        }

    }
}
