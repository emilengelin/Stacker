﻿using UnityEngine;

namespace Stacker.Controllers
{

    class CameraController : Controller<CameraController>
    {

        #region Editor

        [SerializeField] private Camera mainCamera;

        #endregion

        #region Public static properties

        public static Camera MainCamera
        {
            get
            {
                return Singleton.mainCamera;
            }
        }

        #endregion

    }

}
