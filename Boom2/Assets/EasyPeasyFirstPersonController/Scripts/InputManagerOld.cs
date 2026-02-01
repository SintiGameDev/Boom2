namespace EasyPeasyFirstPersonController
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;

    public class InputManagerOld : MonoBehaviour
    {
        public Vector2 moveInput;
        public Vector2 lookInput;
        public bool jump;
        public bool sprint;
        public bool crouch;
        public bool slide;
        void Update()
        {
            moveInput = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
            lookInput = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
            jump = Input.GetKey(KeyCode.Space);
            sprint = Input.GetKey(KeyCode.LeftShift);
            crouch = Input.GetKey(KeyCode.LeftControl);
            slide = Input.GetKey(KeyCode.LeftControl);
        }
    }
}