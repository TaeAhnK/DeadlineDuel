using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Rendering;
using UnityEngine;

namespace Boss.Skills
{
    
    public class SkillIndicator : MonoBehaviour
    {
        public event Action OnIndicatorComplete;
        
        private Renderer _renderer;
        private static readonly int Angle = Shader.PropertyToID("_Angle");
        private static readonly int Duration = Shader.PropertyToID("_Duration");
        
        private void Awake()
        {
            TryGetComponent(out Renderer rend);
            this._renderer = rend;
            Off();
        }

        public void On()
        {
            _renderer.enabled = true;
        }

        public void Off()
        {
            _renderer.enabled = false;
        }
        
        public void SetAngle(float angle)
        {
            this._renderer.material.SetFloat(Angle, angle);
        }

        public void SetLocation(Vector3 location)
        {
            location.y += 0.01f;
            gameObject.transform.position = location;
        }
        
        public void ActivateIndicator(Vector3 location, float angle, float seconds, float fillStart)
        {
            SetLocation(location);
            SetAngle(angle);
            On();
            SetFill(seconds, fillStart);
        }
        
        public void SetFill(float seconds, float fillStart)
        {
            StartCoroutine(IncreaseDuration(seconds, fillStart));
        }

        private void IndicatorComplete()
        {
            Off();
            OnIndicatorComplete?.Invoke();
        }
        
        private IEnumerator IncreaseDuration(float seconds, float fillStart)
        {
            float elapsedTime = fillStart;
            while (elapsedTime < seconds)
            {
                _renderer.material.SetFloat(Duration, elapsedTime / seconds);
                elapsedTime += Time.deltaTime;
                yield return null;
            }
            _renderer.material.SetFloat(Duration, 1f);
            IndicatorComplete();
        }
        
    }
}