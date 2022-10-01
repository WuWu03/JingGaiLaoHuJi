using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PanelLaoHuJi : MonoBehaviour
{
    public GameObject item;
    public GameObject grid;
    public GameObject core;
    public GameObject jinggaiWin;
    public GameObject jinggaiLose;
    public GameObject result;
    public AudioClip startClip;
    public AudioClip winClip;
    public AudioClip loseClip;
    public AudioClip stopClip;
    public AudioSource source;

    public Toggle toggle;
    public Button btnStart;

    public float delay = 0.5f;
    public float startSpeed = 120f;
    public float endSpeed = 360f;
    public float acceleration = 50f;
    public float previewSpeed = 120f;

    public int stopMaxRound = 1;
    public int column = 3;
    public int row = 9;

    enum State
    {
        None,
        Preview,
        Play,
        Result,
        DelayToPreview,
    }

    private void Awake()
    {
        item.gameObject.SetActive(false);
        item.transform.GetChild(0).gameObject.SetActive(false);

        item.GetComponent<VerticalLayoutGroup>().childControlWidth = true;
        item.GetComponent<VerticalLayoutGroup>().childControlHeight = true;
        item.GetComponent<VerticalLayoutGroup>().childForceExpandWidth = false;
        item.GetComponent<VerticalLayoutGroup>().childForceExpandHeight = false;

        grid.GetComponent<HorizontalLayoutGroup>().childControlWidth = true;
        grid.GetComponent<HorizontalLayoutGroup>().childControlHeight = true;
        grid.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = false;
        grid.GetComponent<HorizontalLayoutGroup>().childForceExpandHeight = false;

        m_ImageItems = new GameObject[column][];
        m_ListItems = new List<GameObject>();
        m_Delays = new float[column];
        m_Results = new int[column];
        m_Rewards = new int[column];
        m_EndAcceleration = new float[column];
        m_StartSpeed = new float[column];
        m_EndSpeed = new float[column];

        for (int i = 0; i < column; i++)
        {
            m_ImageItems[i] = new GameObject[row];
            GameObject go = GameObject.Instantiate(item, grid.transform, false);
            go.name = (i + 1).ToString();
            go.SetActive(true);
            m_ListItems.Add(go);
            for (int j = 0; j < row; j++)
            {
                Transform img = go.transform.GetChild(0);
                m_ImageItems[i][j] = GameObject.Instantiate(img.gameObject, go.transform, false);
                m_ImageItems[i][j].name = (j + 1).ToString();
                m_ImageItems[i][j].SetActive(true);

                int imageIndex = j % 5 + 1;
                m_ImageItems[i][j].GetComponent<Image>().sprite = Resources.Load<Sprite>(imageIndex.ToString());
            }
        }

        result.gameObject.SetActive(false);
        jinggaiWin.SetActive(false);
        jinggaiLose.SetActive(false);
        btnStart.onClick.AddListener(OnBtnStartClick);
        toggle.onValueChanged.AddListener(OnToggleValueChanged);
    }

    private void Start()
    {
        StartCoroutine(Init());
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.N))
        {
            m_IsCheat = !m_IsCheat;
        }

        if (m_State == State.Play)
            Play();
        else if (m_State == State.Preview)
            Preivew();
        else if (m_State == State.Result)
            Result();
        else if (m_State == State.DelayToPreview)
            DelayToPreview();
    }

    private IEnumerator Init()
    {
        yield return null;
        m_TopPos = m_ImageItems[column / 2][0].GetComponent<RectTransform>().anchoredPosition3D.y;
        m_MiddlePos = m_ImageItems[column / 2][row / 2].GetComponent<RectTransform>().anchoredPosition3D.y;
        m_BottomPos = m_ImageItems[column / 2][row - 1].GetComponent<RectTransform>().anchoredPosition3D.y - 60;
        SetLayoutChildControl(false);
        m_State = State.Preview;
    }

    private void Preivew()
    {
        result.gameObject.SetActive(false);
        jinggaiWin.SetActive(false);
        jinggaiLose.SetActive(false);
        core.SetActive(true);

        for (int i = 0; i < column; i++)
        {
            m_ListItems[i].GetComponent<VerticalLayoutGroup>().enabled = false;
            UpdatePos(i, previewSpeed);
        }
    }

    private void Play()
    {
        m_StartPlayComplete = true;

        for (int i = 0; i < column; i++)
        {
            m_ListItems[i].GetComponent<VerticalLayoutGroup>().enabled = false;
            float t = Time.time - m_Delays[i];

            if (t < 0)
            {
                m_StartPlayComplete = false;
                UpdatePos(i, previewSpeed);
            }
            else
            {
                m_StartSpeed[i] = CaculateSpeed(startSpeed, acceleration, t);
                m_StartSpeed[i] = m_StartSpeed[i] > endSpeed ? endSpeed : m_StartSpeed[i];
                UpdatePos(i, m_StartSpeed[i]);
            }
        }

        if(m_StartPlayComplete)
        {
            btnStart.transform.GetChild(0).GetComponent<Text>().text = "停止";
        }
    }

    private void Result()
    {
        for (int i = 0; i < column; i++)
        {
            m_ListItems[i].GetComponent<VerticalLayoutGroup>().enabled = false;
            float t = Time.time - m_Delays[i];

            if (t < 0)
            {
                m_StartSpeed[i] = CaculateSpeed(startSpeed, acceleration, t);
                m_StartSpeed[i] = m_StartSpeed[i] > endSpeed ? endSpeed : m_StartSpeed[i];
                UpdatePos(i, m_StartSpeed[i]);
            }
            else
            {
                result.gameObject.SetActive(true);

                if (m_EndAcceleration[i] == -1)
                {
                    m_EndAcceleration[i] = CaculateEndAcceleration(i, 0, m_StartSpeed[i]);
                }

                m_EndSpeed[i] = CaculateSpeed(m_StartSpeed[i], m_EndAcceleration[i], t);
                m_EndSpeed[i] = m_EndSpeed[i] < 0 ? 0 : m_EndSpeed[i];

                if (m_EndSpeed[i] <= 0)
                {
                    RectTransform resultTarget = m_ImageItems[i][m_Results[i]].GetComponent<RectTransform>();

                    if (Mathf.Abs(resultTarget.anchoredPosition3D.y - m_MiddlePos) > 0f)
                        m_EndSpeed[i] = resultTarget.anchoredPosition3D.y > m_MiddlePos ? 5 : -5;
                }

                UpdatePos(i, m_EndSpeed[i]);
            }
        }

        Stop();
    }

    private void Stop()
    {
        for (int i = 0; i < column; i++)
        {
            if (m_EndSpeed[i] > 0)
            {
                return;
            }
        }

        for (int i = 0; i < column; i++)
        {
            for (int j = 0; j < m_ImageItems[i].Length; j++)
            {
                RectTransform transform = m_ImageItems[i][j].GetComponent<RectTransform>();
                if (Mathf.Abs(transform.anchoredPosition3D.y - m_BottomPos) < 5f)
                {
                    transform.anchoredPosition3D = new Vector3(transform.anchoredPosition3D.x, m_TopPos, transform.anchoredPosition3D.z);
                    transform.SetAsFirstSibling();
                }
            }

            m_ListItems[i].GetComponent<VerticalLayoutGroup>().enabled = true;
        }

        btnStart.transform.GetChild(0).GetComponent<Text>().text = "请查看结果";
        m_DelayTimer = Time.time;
        m_State = State.DelayToPreview;
    }

    private void DelayToPreview()
    {
        if (!m_IsPlayEnd)
        {
            bool isWin = true;

            for (int i = 0; i < column; i++)
            {
                if (m_Results[i] != m_Rewards[i])
                {
                    isWin = false;
                    break;
                }
            }

            jinggaiWin.SetActive(isWin);
            jinggaiLose.SetActive(!isWin);
            jinggaiWin.transform.GetChild(0).gameObject.SetActive(isWin && toggle.isOn);
            jinggaiWin.transform.GetChild(1).gameObject.SetActive(isWin && !toggle.isOn);
            core.SetActive(false);

            if (isWin)
            {
                source.clip = winClip;
            }
            else
            {
                source.clip = loseClip;
            }

            source.Play();
            m_IsPlayEnd = true;
        }

        if (Time.time - m_DelayTimer >= 5f)
        {
            m_State = State.Preview;
            btnStart.transform.GetChild(0).GetComponent<Text>().text = "开始";
            toggle.interactable = true;
            toggle.isOn = false;
            m_IsCheat = false;
        }
    }

    private float CaculateSpeed(float v0, float a, float t)
    {
        float realSpeed = v0 + a * t;
        return realSpeed;
    }

    private float CaculateEndAcceleration(int column, float vt, float v0)
    {
        RectTransform target = m_ImageItems[column][m_Results[column]].GetComponent<RectTransform>();
        float currPos = target.anchoredPosition3D.y;
        int round = UnityEngine.Random.Range(0, stopMaxRound);

        float s0 = Mathf.Abs(m_BottomPos - m_TopPos);
        float s1 = Mathf.Abs(m_BottomPos - currPos);
        float s2 = s0 * round;
        float s3 = Mathf.Abs(m_MiddlePos - m_TopPos);

        float s = s1 + s2 + s3;

        return (vt * vt - v0 * v0) / (2 * s);
    }

    private void UpdatePos(int column,float speed)
    {
        for (int j = 0; j < m_ImageItems[column].Length; j++)
        {
            RectTransform transform = m_ImageItems[column][j].GetComponent<RectTransform>();

            float x = transform.anchoredPosition3D.x;
            float y = transform.anchoredPosition3D.y;
            float z = transform.anchoredPosition3D.z;

            if (speed <= 0)
            {
                transform.anchoredPosition3D = new Vector3(x, Mathf.Ceil(y), z);
            }

            if (y > m_BottomPos)
            {
                transform.anchoredPosition3D -= Vector3.up * speed * Time.deltaTime;
            }
            else
            {
                transform.anchoredPosition3D = new Vector3(x, m_TopPos, z);
                transform.SetAsFirstSibling();
            }
        }
    }

    private void OnBtnStartClick()
    {
        if (m_State == State.Preview)
        {
            for (int i = 0; i < column; i++)
            {
                m_Delays[i] = Time.time + i * delay;
            }

            m_IsPlayEnd = false;
            source.clip = startClip;
            source.Play();
            m_State = State.Play;
            toggle.interactable = false;
        }
        else if(m_State == State.Play && m_StartPlayComplete)
        {
            for (int i = 0; i < column; i++)
            {
                m_EndAcceleration[i] = -1;            
                m_Rewards[i] = UnityEngine.Random.Range(0, row - 1);

                if (toggle.isOn || m_IsCheat)
                {
                    m_Results[i] = m_Rewards[i];
                }
                else
                {
                    m_Results[i] = UnityEngine.Random.Range(0, row - 1);
                }

                m_Delays[i] = Time.time + i * delay;
                result.transform.GetChild(i).GetComponent<Image>().sprite = Resources.Load<Sprite>((m_Rewards[i] + 1).ToString());
            }

            btnStart.transform.GetChild(0).GetComponent<Text>().text = "等待中……";
            m_State = State.Result;
            source.clip = stopClip;
            source.Play();
        }
    }

    private void OnToggleValueChanged(bool arg0)
    {
        //m_IsCheat = arg0;
    }

    private void SetLayoutChildControl(bool isControl)
    {
        HorizontalLayoutGroup gridHorizontalLayout = grid.GetComponent<HorizontalLayoutGroup>();
        gridHorizontalLayout.childControlWidth = isControl;
        gridHorizontalLayout.childControlHeight = isControl;
        gridHorizontalLayout.childForceExpandWidth = !isControl;
        gridHorizontalLayout.childForceExpandHeight = !isControl;

        for (int i = 0; i < m_ListItems.Count; i++)
        {
            VerticalLayoutGroup itemVerticalLayout = m_ListItems[i].GetComponent<VerticalLayoutGroup>();
            itemVerticalLayout.childControlWidth = isControl;
            itemVerticalLayout.childControlHeight = isControl;
            itemVerticalLayout.childForceExpandWidth = !isControl;
            itemVerticalLayout.childForceExpandHeight = !isControl;
        }
    }

    private bool m_IsPlayEnd = false;
    private float m_TopPos = 0f;
    private float m_MiddlePos = 0f;
    private float m_BottomPos = 0f;
    private float m_DelayTimer = 0f;
    private float[] m_StartSpeed = null;
    private float[] m_EndSpeed = null;
    private float[] m_Delays = null;
    private int[] m_Results = null;
    private int[] m_Rewards = null;
    private float[] m_EndAcceleration = null;
    private bool m_StartPlayComplete = false;
    private bool m_IsCheat = false;
    private State m_State = State.None;
    private List<GameObject> m_ListItems = null;
    private GameObject[][] m_ImageItems = null;
}
