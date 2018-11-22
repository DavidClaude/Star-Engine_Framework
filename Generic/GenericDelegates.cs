﻿using System;

namespace DacLib.Generic
{
    #region generic delegates
    public delegate void NoneForVoid_Handler();
    public delegate void IntForVoid_Handler(int i);
    public delegate void StringForVoid_Handler(string s);
    public delegate void BytesForVoid_Handler(byte[] data);
    #endregion

    #region U3D delegates
    public delegate void GameObjectForVoid_Handler(UnityEngine.GameObject gameObj);
    #endregion
}