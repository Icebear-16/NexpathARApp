using System;
using System.Collections.Generic;
using UnityEngine;
public static class DbSeedData
{
    public static readonly string[] Names = new string[] {
        "L1_R109","L1_R107","L1_R108","L1_R106","L1_R103","L1_R104","L1_R105",
        "L1_R101","L1_R100","L1_T1","L1_CorA","L1_CorB","L1_CorC","L1_CorD",
        "L1_CorE","L1_T2","L1_AT01_EXIT","L1_AT02_ENTRY","STAIR_L1_BOT",
        "STAIR_L2_MID","STAIR_L2_TOP","L2_AT02_EXIT","L2_AT03_ENTRY","L2_R208",
        "L2_R207","L2_R206","L2_R200","L2_CorA","L2_CorB","L2_T3","L2_R205",
        "L2_R202","L2_CorC","L2_CorD","L2_T4","L2_AT04_ENTRY",
        "L2_AT03_EXIT","L2_AT04_EXIT" };
    public static readonly float[] Xs = new float[] {
        13.1334f,14.2444f,17.5104f,10.1124f,8.3654f,8.3654f,8.3654f,4.3094f,
        4.3094f,8.4134f,11.8504f,19.3344f,9.9744f,5.4644003f,15.2154f,10.3674f,
        9.2524f,8.8304f,8.3254f,4.7544003f,8.8364f,8.9794f,9.5224f,13.3234f,
        17.5104f,14.2444f,4.3094f,11.8504f,19.3344f,10.6164f,9.0544f,6.2544003f,
        9.9744f,5.4644003f,8.6954f,9.1944f,
        11.46f,10.89f };
    public static readonly float[] Ys = new float[] {
        -1.5f,-1.5f,-1.5f,-1.5f,-1.5f,-1.5f,-1.5f,-1.5f,-1.5f,-1.5f,-1.5f,
        -1.5f,-1.5f,-1.5f,-1.5f,-1.5f,-1.5f,-1.5f,-1.5f,0.116f,1.55f,1.55f,
        1.55f,1.65f,1.65f,1.65f,1.5f,1.5f,1.65f,1.55f,1.5f,1.5f,1.5f,1.5f,
        1.55f,1.55f,
        1.50f,1.50f };
    public static readonly float[] Zs = new float[] {
        -9.02426f,-10.27526f,-5.31126f,-16.93526f,-20.07626f,-20.07626f,
        -20.07626f,-22.331259f,-22.331259f,-15.04826f,-13.24926f,-1.40426f,
        -15.813259f,-23.36326f,-16.24826f,-12.410259f,-14.34226f,-14.10026f,
        -13.91526f,-10.70326f,-12.24226f,-12.632259f,-12.646259f,-9.46026f,
        -5.31126f,-10.27526f,-22.331259f,-13.24926f,-1.40426f,-12.383261f,
        -18.37326f,-23.01326f,-15.813259f,-23.36326f,-14.69426f,-13.421261f,
        -13.91f,-14.79f };
    public static readonly string[] Types = new string[] {
        "room","room","room","room","room","room","room","room","room",
        "junction","corner","corner","corner","corner","corner","junction",
        "entrance","entrance","stair","stair","stair","entrance","entrance",
        "room","room","room","room","corner","corner","junction","room",
        "room","corner","corner","junction","entrance",
        "entrance","entrance" };
    public static readonly int[] Floors = new int[] {
        1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,
        2,2 };
    public static readonly string[] Areas = new string[] {
        "AT_01","AT_01","AT_01","AT_01","AT_01","AT_01","AT_01","AT_01","AT_01",
        "AT_01","AT_01","AT_01","AT_01","AT_01","AT_01","AT_01","AT_01","AT_02",
        "AT_02","AT_02","AT_02","AT_02","AT_03","AT_03","AT_03","AT_03","AT_03",
        "AT_03","AT_03","AT_03","AT_04","AT_04","AT_04","AT_04","AT_04","AT_04",
        "AT_03","AT_04" };
    public static readonly int[] Trans = new int[] {
        0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1,1,1,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,
        1,1 };
}
