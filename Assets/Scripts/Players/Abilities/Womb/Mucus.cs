using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Mucus : MonoBehaviour
{
    [SerializeField] private GameObject mucus;
    [SerializeField] private ObjectHealth mucusHealth;

    public GameObject MucusObject { get => mucus; set => mucus = value; }
    public ObjectHealth MucusHeath { get => mucusHealth; set => mucusHealth = value; }
}
