using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

using System.Linq;

namespace Stacker
{
    public class Stack : MonoBehaviour
    {
        public Text scoreText;
        public Color32[] gameColors;
        public Material stackMat;
        public GameObject endPanel;
        public AudioClip[] clips;

        public float boundsSize = 3.5f;
        public float stackMovingSpeed = 5.0f;
        public float errorMargin = 0.25f;
        public float stackBoundsGain = 0.25f;
        public int comboStartGain = 3;

        private Transform[] stack;
        private Vector3 stackBounds;

        private int prevIndex;
        private int currIndex;
        public int scoreCount = 0;
        private int combo = 0;
        private int lastColorIndex = 0;
        private Color32 startColor;
        private Color32 endColor;

        private float tileTransition = 0.0f;
        private float tileSpeed = 2.5f;
        private float secondaryPosition;
        private float colorTransition = 0;

        private bool isMovingOnX = true;
        private bool gameOver = false;

        private Vector3 desiredPosition;

        private void Start()
        {
            stack = new Transform[transform.childCount];
            startColor = gameColors.First();
            endColor = gameColors.Last();
            lastColorIndex = 1;
            for (int i = 0; i < transform.childCount; i++)
            {
                Transform tile = transform.GetChild(i);
                stack[i] = tile;

                MeshFilter filter = tile.GetComponent<MeshFilter>();
                ColorMesh(filter.mesh);
            }
            currIndex = transform.childCount - 1;

            stackBounds = new Vector3(boundsSize, 1, boundsSize);
        }

        private void CreateRubble(Vector3 pos, Vector3 scale)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            go.AddComponent<Rigidbody>();

            go.GetComponent<MeshRenderer>().material = stackMat;
            ColorMesh(go.GetComponent<MeshFilter>().mesh);
        }

        private void Update()
        {
            if (gameOver)
                return;

            MoveTile();

            // Move the stack
            transform.position = Vector3.Lerp(transform.position, desiredPosition, stackMovingSpeed * Time.deltaTime);
        }

        private void MoveTile()
        {
            tileTransition += Time.deltaTime * tileSpeed;
            float movement = Mathf.Sin(tileTransition) * boundsSize;

            Transform currTile = stack[currIndex];

            if (isMovingOnX)
                currTile.localPosition = new Vector3(movement, scoreCount, secondaryPosition);
            else
                currTile.localPosition = new Vector3(secondaryPosition, scoreCount, movement);
        }

        public void SpawnTile()
        {
            Transform currTile = stack[currIndex];

            tileTransition = 1.0f;

            prevIndex = currIndex;
            currIndex--;
            if (currIndex < 0)
                currIndex = transform.childCount - 1;

            currTile = stack[currIndex];

            desiredPosition = (Vector3.down) * scoreCount;
            currTile.localPosition = new Vector3(0, scoreCount, 0);
            currTile.localScale = new Vector3(stackBounds.x, 1, stackBounds.z);

            MeshFilter filter = currTile.GetComponent<MeshFilter>();
            ColorMesh(filter.mesh);
        }

        private void ScaleUpTile(bool scaleX = true)
        {
            Transform prevTile = stack[prevIndex];
            Transform currTile = stack[currIndex];

            if (scaleX)
            {
                stackBounds.x += stackBoundsGain;
                if (stackBounds.x > boundsSize)
                    stackBounds.x = boundsSize;
            }
            else
            {
                stackBounds.z += stackBoundsGain;
                if (stackBounds.z > boundsSize)
                    stackBounds.z = boundsSize;
            }

            float prevPos = scaleX ? prevTile.localPosition.x : prevTile.localPosition.z;
            float currPos = scaleX ? currTile.localPosition.x : currTile.localPosition.z;

            float halfPrevScale = scaleX ? prevTile.localScale.x * 0.5f : prevTile.localScale.z * 0.5f;
            float halfCurrScale = scaleX ? currTile.localScale.x * 0.5f : currTile.localScale.z * 0.5f;

            float halfPrevPos = prevPos / 2;
            float halfCurrPos = currPos / 2;

            float middle = prevPos + halfCurrPos;
            currTile.localScale = new Vector3(stackBounds.x, 1, stackBounds.z);

            if (scaleX)
            {
                currTile.localPosition = new Vector3(middle - halfPrevPos, scoreCount, prevTile.localPosition.z);
            }
            else
            {
                currTile.localPosition = new Vector3(prevTile.localPosition.x, scoreCount, middle - halfPrevPos);
            }
        }
        private void CutTile(bool cutX = true)
        {
            Transform prevTile = stack[prevIndex];
            Transform currTile = stack[currIndex];

            float delta = cutX ? prevTile.localPosition.x - currTile.localPosition.x : prevTile.localPosition.z - currTile.localPosition.z;

            float prevPos = cutX ? prevTile.localPosition.x : prevTile.localPosition.z;
            float currPos = cutX ? currTile.localPosition.x : currTile.localPosition.z;

            float halfPrevScale = cutX ? prevTile.localScale.x * 0.5f : prevTile.localScale.z * 0.5f;
            float halfCurrScale = cutX ? currTile.localScale.x * 0.5f : currTile.localScale.z * 0.5f;

            float halfPrevPos = prevPos / 2;
            float halfCurrPos = currPos / 2;

            float middle = prevPos + halfCurrPos;
            currTile.localScale = new Vector3(stackBounds.x, 1, stackBounds.z);

            if (cutX)
            {
                CreateRubble
                (
                    new Vector3((currTile.position.x > 0)
                        ? currTile.position.x + halfCurrScale
                        : currTile.position.x - halfCurrScale
                        , currTile.position.y
                        , currTile.position.z),
                    new Vector3(Mathf.Abs(delta), 1, currTile.localScale.z)
                );

                currTile.localPosition = new Vector3(middle - halfPrevPos, scoreCount, prevTile.position.z);
            }
            else
            {
                CreateRubble
                (
                    new Vector3(currTile.position.x, currTile.position.y,
                    (currTile.position.z > 0)
                        ? currTile.position.z + halfCurrScale
                        : currTile.position.z - halfCurrScale),
                    new Vector3(currTile.localScale.x, 1, Mathf.Abs(delta))
                );

                currTile.localPosition = new Vector3(prevTile.position.x, scoreCount, middle - halfPrevPos);
            }
        }

        private bool ValidateTile(bool checkX = false)
        {
            Transform prevTile = stack[prevIndex];
            Transform currTile = stack[currIndex];

            float delta = checkX ? prevTile.position.x - currTile.position.x : 
                                   prevTile.position.z - currTile.position.z;
            if (Mathf.Abs(delta) > errorMargin)
            {
                // Put something in the clips array before un-commenting this line
                //	AudioSource.PlayClipAtPoint (clips [0], Camera.main.transform.position);
                // CUT THE TILE
                combo = 0;

                if (checkX)
                {
                    stackBounds.x -= Mathf.Abs(delta);
                    if (stackBounds.x <= 0)
                        return false;
                }
                else
                {
                    stackBounds.z -= Mathf.Abs(delta);
                    if (stackBounds.x <= 0)
                        return false;
                }

                CutTile(checkX);
            }
            else
            {
                // Put something in the clips array before un-commenting this line
                // AudioSource.PlayClipAtPoint (clips [1], Camera.main.transform.position);
                if (combo > comboStartGain)
                {
                    ScaleUpTile(checkX);
                }

                combo++;
                currTile.localPosition = new Vector3(prevTile.localPosition.x, scoreCount, prevTile.localPosition.z);
            }

            return true;
        }

        public bool PlaceTile()
        {
            bool isValidTile = ValidateTile(isMovingOnX);
            if (!isValidTile)
                return false;

            Transform prevTile = stack[prevIndex];
            Transform currTile = stack[currIndex];

            secondaryPosition = (isMovingOnX)
                ? currTile.localPosition.x
                : currTile.localPosition.z;
            isMovingOnX = !isMovingOnX;

            return true;
        }

        private void ColorMesh(Mesh mesh)
        {
            Vector3[] vertices = mesh.vertices;
            Color32[] colors = new Color32[vertices.Length];
            colorTransition += 0.1f;
            if (colorTransition > 1)
            {
                colorTransition = 0.0f;
                startColor = endColor;
                int ci = lastColorIndex;
                while (ci == lastColorIndex)
                    ci = Random.Range(0, gameColors.Length);
                endColor = gameColors[ci];
            }
            Color c = Color.Lerp(startColor, endColor, colorTransition);

            for (int i = 0; i < vertices.Length; i++)
                colors[i] = c;

            mesh.colors32 = colors;
        }

        public void EndGame()
        {
            if (PlayerPrefs.GetInt("score") < scoreCount)
                PlayerPrefs.SetInt("score", scoreCount);
            gameOver = true;
            endPanel.SetActive(true);
            stack[currIndex].gameObject.AddComponent<Rigidbody>();

#if UNITY_EDITOR
#elif UNITY_ANDROID
		if (Admob.Instance().isRewardedVideoReady()) {
		Admob.Instance().showRewardedVideo();
		}
#elif UNITY_IPHONE
		if (Admob.Instance().isRewardedVideoReady()) {
		Admob.Instance().showRewardedVideo();
		}
#endif
        }

        public void OnButtonClick(string sceneName)
        {
            SceneManager.LoadScene(sceneName);
        }
    }
}