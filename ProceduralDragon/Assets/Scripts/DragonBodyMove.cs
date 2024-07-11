using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;

public class DragonBodyMove : MonoBehaviour {
    [SerializeField] private float lineThickness = 1f;
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private Attachments[] attachments;
    [SerializeField] private Transform rotVelTarget;
    [SerializeField] private VelocityToRot[] rotVelAttachments;
    [SerializeField] private Wings[] wings;
    [SerializeField] private Transform target;
    [SerializeField] private float smoothness = .5f;
    [SerializeField] private float waveMoveSpeed = 10f;
    [SerializeField] private BodyType bodyType = BodyType.Square;
    [SerializeField] private int resolution = 7;
    [SerializeField] private float amplitude = 2.0f; 
    [SerializeField] private float frequency = 0.68f; 
    [SerializeField] private float length = 16.89f;
    [SerializeField] private float offset = 0.0f;
    [SerializeField] private Vector3 offsetPoints;
    [SerializeField] private AnimationCurve movementPerSegmentY;
    
    private Vector3[] _points;
    private Vector3[] _additionalYOffset;
    private Vector3 _originTargetPos;
    private Vector3 _lastFramePos;
    private float _currentSpeed;
    private Vector3 _currentVelocity;

    
    
    private void Start() {
        _points = new Vector3[resolution];
        _additionalYOffset = new Vector3[resolution];
        _originTargetPos = target.position;
        
        lineRenderer.positionCount = resolution;
    }

    private void Update() {
        AnimateTheDragon();
        SetLineThickness();
    }
    
    private void AnimateTheDragon() {
        SetPointsBasedOnWave();
        SmoothFollowPoints();
        DrawLine();
        ApplyAttachments();
        ApplyVelocityToRotation();
        ApplyWingRotation();
    }

    private float _smoothRemapRot;
    private void ApplyWingRotation() {
        foreach (var wing in wings) {
            var remappedSpeed = Remap(0, 100, wing.speedMinMax.x, wing.speedMinMax.y, _currentSpeed) * wing.speed ;
            var remappedRotation = Remap(0, 100, 1, 2, _currentSpeed);
            _smoothRemapRot = Mathf.Lerp( _smoothRemapRot, remappedRotation, Time.deltaTime * 10f );
            var rotation = Mathf.Sin( Time.time * remappedSpeed ) * (wing.maxRotation * _smoothRemapRot);
            var smoothRotation = Mathf.LerpAngle(wing.wing.localEulerAngles.z, rotation, Time.deltaTime * (wing.smoothing * remappedSpeed));
            wing.wing.localEulerAngles = new Vector3(0, smoothRotation, 0);
        }
    }


    private void ApplyAttachments() {
        foreach (var attachment in attachments) {
            var pointIndex = attachment.attachmentIndex;
            var point = _points[pointIndex] + _additionalYOffset[pointIndex];
            var position = point + attachment.offset;
            
            attachment.attachment.localPosition = position;
            if (attachment.applyRotation) {
                var rotation = Quaternion.Euler(0, 0, point.y * attachment.rotationOffset);
                attachment.attachment.rotation = rotation;
            }
        }
    }
    
    private void ApplyVelocityToRotation() {
        var currentPos = rotVelTarget.position;
        var distanceMoved = Vector3.Distance(currentPos, _lastFramePos);
        _currentSpeed = distanceMoved / Time.deltaTime;
        _currentVelocity = (currentPos - _lastFramePos) / Time.deltaTime;
        foreach (var rotVelAttachment in rotVelAttachments) {
            
            float rotationZ = Mathf.Clamp(
                _currentVelocity.y * rotVelAttachment.speed,
                rotVelAttachment.range.x,
                rotVelAttachment.range.y);
            
            if (rotVelAttachment.reverse) {
                rotationZ *= -1;
            }
            
            float smoothedRotation = Mathf.LerpAngle(rotVelAttachment.attachment.localEulerAngles.z, rotationZ, Time.deltaTime * rotVelAttachment.smoothing);
            rotVelAttachment.attachment.localEulerAngles = new Vector3(0, 0, smoothedRotation);
            
        }
        _lastFramePos = currentPos;
    }

    private void DrawLine() {
        for ( int i = 0; i < resolution; i++ ) {
            lineRenderer.SetPosition( i, _additionalYOffset[ i ] + _points[i] );
        }
    }

    private void SetPointsBasedOnWave() {
        switch (bodyType) {
            case BodyType.Sin:
                DrawSineWave();
                break;
            case BodyType.Square:
                DrawSquareWave();
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    void DrawSineWave() {
        float xIncrement = length / (resolution - 1);
        offset += Time.deltaTime * waveMoveSpeed;
        for (int i = 0; i < resolution; i++) {
            float x = i * xIncrement;
            float y = amplitude * Mathf.Sin(frequency * (x + offset));
            _points[i] = new Vector3(x, y, 0);
        }
    }
    
    void DrawSquareWave() {
        float xIncrement = length / (resolution - 1);
        offset += Time.deltaTime * waveMoveSpeed;
        for (int i = 0; i < resolution; i++) {
            float x = i * xIncrement;
            float y = amplitude * Mathf.Sign(Mathf.Sin(frequency * (x + offset)));
            float remappedI = Remap(0, resolution, 1, 0, i);
            float evaluatedMovement = movementPerSegmentY.Evaluate(remappedI);
            _points[i] = new Vector3(x, y * evaluatedMovement, 0);
            
            
        }
        
        
    }
    
    private void SmoothFollowPoints() {
        var delta = target.position - _originTargetPos;
        for (int i = 0; i < resolution; i++) {
            var smoothingIndex = i + 3;
            _additionalYOffset[i] = Vector3.Lerp( _additionalYOffset[i], delta, Time.deltaTime * (smoothness * smoothingIndex) );
        }
    }
    
    private void SetLineThickness() {
        lineRenderer.startWidth = lineThickness;
        lineRenderer.endWidth = lineThickness;
    }
    
    public static float Remap(float iMin, float iMax, float oMin, float oMax, float v) {
        var t = Mathf.InverseLerp(iMin, iMax, v);
        return Mathf.Lerp(oMin, oMax, t);
    }
    
    public Vector3[] GetPoints() {
        return _points;
    }
    
    public Vector3 GetPoint( int index ) {
        return _points[index];
    }
    
    public Vector3[] GetAdditionalOffsets() {
        return _additionalYOffset;
    }
    
    public Vector3 GetAdditionalOffset( int index ) {
        return _additionalYOffset[index];
    }
    
    public int GetResolution() {
        return resolution;
    }
    
    [Serializable]
    public enum BodyType {
        Sin,
        Square,
    }
    
    [Serializable]
    public class Attachments {
        public Transform attachment;
        public Vector3 offset;
        public float rotationOffset;
        public int attachmentIndex;
        public bool applyRotation;
    }
    
    [Serializable]
    public class VelocityToRot {
        public Transform attachment;
        public float offset;
        public Vector2 range;
        public float speed;
        public float smoothing;
        public bool reverse;
    }
    
    [Serializable]
    public class Wings {
        public Transform wing;
        public Vector2 speedMinMax;
        public float speed;
        public float maxRotation;
        public float smoothing;
    }
}
