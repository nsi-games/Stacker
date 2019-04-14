using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Stacker
{
    [RequireComponent(typeof(Stack))]
    public class UserInput : MonoBehaviour
    {
        private Stack stack;
        // Use this for initialization
        void Start()
        {
            stack = GetComponent<Stack>();
        }

        // Update is called once per frame
        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                if (stack.PlaceTile())
                {
                    stack.SpawnTile();
                    stack.scoreCount++;
                 //   stack.scoreText.text = stack.scoreCount.ToString();
                }
                else
                {
                    stack.EndGame();
                }
            }
        }
    }
}