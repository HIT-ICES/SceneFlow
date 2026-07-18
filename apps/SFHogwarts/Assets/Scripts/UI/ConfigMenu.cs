using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityStandardAssets.CinematicEffects;

public class ConfigMenu : MonoBehaviour
{
    public Toggle bloom;
    public GameObject dev;
    public Toggle dof;
    public GameObject lightSlider;
    public Toggle map;

    public GameObject menu;
    public GameObject[] panel;
    public Toggle performance;
    public GameObject perftext;
    public GameObject player;
    public Dropdown qdrop;
    public GameObject rightbar;
    public Toggle ssao;


    private void LoadCfg()
    {
        //ssao.isOn = Camera.main.GetComponent<SESSAO> ().enabled;
        try
        {
            dof.isOn = Camera.main.GetComponent<DepthOfField>().enabled;
        }
        catch (Exception)
        {
        }

        qdrop.value = QualitySettings.GetQualityLevel();
    }

    public void ConfigShow()
    {
        menu.SetActive(!menu.activeSelf);
        LoadCfg();
    }

    public void ConfigDisconnect()
    {
        PhotonNetwork.Disconnect();
        SceneManager.LoadScene("MainMenu");
    }

    public void ConfigQuit()
    {
        Application.Quit();
    }

    public void OptionsSetQuality()
    {
        QualitySettings.SetQualityLevel(qdrop.value);
    }

    public void OptionsSetSSAO()
    {
        //Camera.main.GetComponent<SESSAO> ().enabled = ssao.isOn;
    }

    public void OptionsSetDOF()
    {
        Camera.main.GetComponent<DepthOfField>().enabled = dof.isOn;
    }

    public void OptionsSetBloom()
    {
        Camera.main.GetComponent<Bloom>().enabled = bloom.isOn;
    }

    public void OptionsRightBar()
    {
        //rightbar.SetActive ();
    }

    public void OptionsPerformance()
    {
        perftext.SetActive(performance.isOn);
    }

    public void MapPos()
    {
        if (map.isOn)
            GameObject.Find("Canvas/MiniMap").GetComponent<RectTransform>().anchoredPosition = new Vector3(-6, 10, 1);
        else
            GameObject.Find("Canvas/MiniMap").GetComponent<RectTransform>().anchoredPosition = new Vector3(-6, 66, 1);
    }

    public void ShowPanel(int p)
    {
        for (var i = 0; i < panel.Length; i++) panel[i].SetActive(false);
        panel[p].SetActive(true);
    }

    public void Respawn()
    {
        player.GetComponent<Player>().Respawn();
    }

    public void AddHP(int n)
    {
        Player.Instance.health += n;
    }

    public void updateDaytime()
    {
        try
        {
            GameObject.Find("Lights").GetComponent<NightSlider>().slider = lightSlider.GetComponent<Slider>().value;
        }
        catch (Exception)
        {
        }
    }
}