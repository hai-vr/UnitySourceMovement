using System;
using UnityEngine;
using Fragsurf.TraceUtil;
using UdonSharp;

namespace Fragsurf.Movement {
    public class SurfController : UdonSharpBehaviour {
        // Udon Specific
        public Trace traceHolder;
        public SurfPhysics surfPhysicsHolder;

        ///// Fields /////

        [HideInInspector] public Transform playerTransform;
        private ISurfControllable _surfer;
        private MovementConfig _config;
        private float _deltaTime;

        public bool jumping = false;
        public bool crouching = false;
        public float speed = 0f;
        
        public Transform camera;
        public float cameraYPos = 0f;

        private float slideSpeedCurrent = 0f;
        private Vector3 slideDirection = Vector3.forward;

        private bool sliding = false;
        private bool wasSliding = false;
        private float slideDelay = 0f;
        
        private bool uncrouchDown = false;
        private float crouchLerp = 0f;

        private float frictionMult = 1f;

        ///// Methods /////

        Vector3 groundNormal = Vector3.up;

        /// <summary>
        /// 
        /// </summary>
        public void ProcessMovement (ISurfControllable surfer, MovementConfig config, float deltaTime) {
            // cache instead of passing around parameters
            _surfer = surfer;
            _config = config;
            _deltaTime = deltaTime;

            var surfer_collider = _surfer.XGet_collider();
            var surfer_moveData = _surfer.XGet_moveData();

            if (surfer_moveData.laddersEnabled && !surfer_moveData.climbingLadder) {

                // Look for ladders
                LadderCheck (new Vector3(1f, 0.95f, 1f), surfer_moveData.velocity * Mathf.Clamp (Time.deltaTime * 2f, 0.025f, 0.25f));

            }

            if (surfer_moveData.laddersEnabled && surfer_moveData.climbingLadder) {

                LadderPhysics ();

            } else if (!surfer_moveData.underwater) {

                if (surfer_moveData.velocity.y <= 0f)
                    jumping = false;

                // apply gravity
                if (_surfer.XGet_groundObject() == null) {
                    surfer_moveData.velocity = V3YAdd(surfer_moveData.velocity, _surfer.XGet_baseVelocity().y * _deltaTime - surfer_moveData.gravityFactor * _config.gravity * _deltaTime);
                }

                // input velocity, check for ground
                CheckGrounded ();
                CalculateMovementVelocity ();

            } else {

                // Do underwater logic
                UnderwaterPhysics ();

            }

            float yVel = surfer_moveData.velocity.y;

            var velXZ = surfer_moveData.velocity;
            velXZ.y = 0f;
            var clampXZ = Vector3.ClampMagnitude (velXZ, _config.maxVelocity);
            surfer_moveData.velocity = clampXZ;
            speed =  clampXZ.magnitude;
            surfer_moveData.velocity = V3YAdd(clampXZ, yVel);

            if (surfer_moveData.velocity.sqrMagnitude == 0f) {

                // Do collisions while standing still
                surfPhysicsHolder.ResolveCollisions (traceHolder, surfer_collider, ref surfer_moveData.origin, ref surfer_moveData.velocity, surfer_moveData.rigidbodyPushForce, 1f, surfer_moveData.stepOffset, _surfer);

            } else {

                float maxDistPerFrame = 0.2f;
                Vector3 velocityThisFrame = surfer_moveData.velocity * _deltaTime;
                float velocityDistLeft = velocityThisFrame.magnitude;
                float initialVel = velocityDistLeft;
                while (velocityDistLeft > 0f) {

                    float amountThisLoop = Mathf.Min (maxDistPerFrame, velocityDistLeft);
                    velocityDistLeft -= amountThisLoop;

                    // increment origin
                    Vector3 velThisLoop = velocityThisFrame * (amountThisLoop / initialVel);
                    surfer_moveData.origin += velThisLoop;

                    // don't penetrate walls
                    surfPhysicsHolder.ResolveCollisions (traceHolder, surfer_collider, ref surfer_moveData.origin, ref surfer_moveData.velocity, surfer_moveData.rigidbodyPushForce, amountThisLoop / initialVel, surfer_moveData.stepOffset, _surfer);

                }

            }

            surfer_moveData.groundedTemp = surfer_moveData.grounded;

            _surfer = null;
            
        }

        private Vector3 V3YAdd(Vector3 velocity, float add)
        {
            velocity.y += add;
            return velocity;
        }

        /// <summary>
        /// 
        /// </summary>
        private void CalculateMovementVelocity () {
            var surfer_moveData = _surfer.XGet_moveData();

            // MoveType moveType = _surfer.XGet_moveType();
            MoveType moveType = MoveType.Walk; // Udon Stub: _surfer.XGet_moveType() raises a compiler error in U# 1.0b8
            switch (moveType) {

                case MoveType.Walk:

                if (_surfer.XGet_groundObject() == null) {

                    /*
                    // AIR MOVEMENT
                    */

                    wasSliding = false;

                    // apply movement from input
                    surfer_moveData.velocity += AirInputMovement ();

                    // let the magic happen
                    surfPhysicsHolder.Reflect (traceHolder, ref surfer_moveData.velocity, _surfer.XGet_collider(), surfer_moveData.origin, _deltaTime);

                } else {

                    /*
                    //  GROUND MOVEMENT
                    */

                    // Sliding
                    if (!wasSliding) {

                        slideDirection = new Vector3 (surfer_moveData.velocity.x, 0f, surfer_moveData.velocity.z).normalized;
                        slideSpeedCurrent = Mathf.Max (_config.maximumSlideSpeed, new Vector3 (surfer_moveData.velocity.x, 0f, surfer_moveData.velocity.z).magnitude);

                    }

                    sliding = false;
                    if (surfer_moveData.velocity.magnitude > _config.minimumSlideSpeed && surfer_moveData.slidingEnabled && surfer_moveData.crouching && slideDelay <= 0f) {

                        if (!wasSliding)
                            slideSpeedCurrent = Mathf.Clamp (slideSpeedCurrent * _config.slideSpeedMultiplier, _config.minimumSlideSpeed, _config.maximumSlideSpeed);

                        sliding = true;
                        wasSliding = true;
                        SlideMovement ();
                        return;

                    } else {

                        if (slideDelay > 0f)
                            slideDelay -= _deltaTime;

                        if (wasSliding)
                            slideDelay = _config.slideDelay;

                        wasSliding = false;

                    }
                    
                    float fric = crouching ? _config.crouchFriction : _config.friction;
                    float accel = crouching ? _config.crouchAcceleration : _config.acceleration;
                    float decel = crouching ? _config.crouchDeceleration : _config.deceleration;
                    
                    // Get movement directions
                    Vector3 forward = Vector3.Cross (groundNormal, -playerTransform.right);
                    Vector3 right = Vector3.Cross (groundNormal, forward);

                    float speed = surfer_moveData.sprinting ? _config.sprintSpeed : _config.walkSpeed;
                    if (crouching)
                        speed = _config.crouchSpeed;

                    Vector3 _wishDir;

                    // Jump and friction
                    if (surfer_moveData.wishJump) {

                        ApplyFriction (0.0f, true, true);
                        Jump ();
                        return;

                    } else {

                        ApplyFriction (1.0f * frictionMult, true, true);

                    }

                    float forwardMove = surfer_moveData.verticalAxis;
                    float rightMove = surfer_moveData.horizontalAxis;

                    _wishDir = forwardMove * forward + rightMove * right;
                    _wishDir.Normalize ();
                    Vector3 moveDirNorm = _wishDir;

                    Vector3 forwardVelocity = Vector3.Cross (groundNormal, Quaternion.AngleAxis (-90, Vector3.up) * new Vector3 (surfer_moveData.velocity.x, 0f, surfer_moveData.velocity.z));

                    // Set the target speed of the player
                    float _wishSpeed = _wishDir.magnitude;
                    _wishSpeed *= speed;

                    // Accelerate
                    float yVel = surfer_moveData.velocity.y;
                    Accelerate (_wishDir, _wishSpeed, accel * Mathf.Min (frictionMult, 1f), false);

                    float maxVelocityMagnitude = _config.maxVelocity;
                    surfer_moveData.velocity = V3YAdd(Vector3.ClampMagnitude (new Vector3 (surfer_moveData.velocity.x, 0f, surfer_moveData.velocity.z), maxVelocityMagnitude), yVel);

                    // Calculate how much slopes should affect movement
                    float yVelocityNew = forwardVelocity.normalized.y * new Vector3 (surfer_moveData.velocity.x, 0f, surfer_moveData.velocity.z).magnitude;

                    // Apply the Y-movement from slopes
                    surfer_moveData.velocity = V3YAdd(surfer_moveData.velocity, yVelocityNew * (_wishDir.y < 0f ? 1.2f : 1.0f));
                    float removableYVelocity = surfer_moveData.velocity.y - yVelocityNew;

                }

                break;

                case MoveType.None:
                case MoveType.Noclip:
                case MoveType.Ladder:
                default:
                    break;
            } // END OF SWITCH STATEMENT
        }

        private void UnderwaterPhysics () {
            var surfer_moveData = _surfer.XGet_moveData();
            surfer_moveData.velocity = Vector3.Lerp (surfer_moveData.velocity, Vector3.zero, _config.underwaterVelocityDampening * _deltaTime);

            // Gravity
            if (!CheckGrounded ())
                surfer_moveData.velocity.y -= _config.underwaterGravity * _deltaTime;

            // Swimming upwards
            if (Input.GetButton ("Jump"))
                surfer_moveData.velocity.y += _config.swimUpSpeed * _deltaTime;

            float fric = _config.underwaterFriction;
            float accel = _config.underwaterAcceleration;
            float decel = _config.underwaterDeceleration;

            ApplyFriction (1f, true, false);

            // Get movement directions
            Vector3 forward = Vector3.Cross (groundNormal, -playerTransform.right);
            Vector3 right = Vector3.Cross (groundNormal, forward);

            float speed = _config.underwaterSwimSpeed;

            Vector3 _wishDir;

            float forwardMove = surfer_moveData.verticalAxis;
            float rightMove = surfer_moveData.horizontalAxis;

            _wishDir = forwardMove * forward + rightMove * right;
            _wishDir.Normalize ();
            Vector3 moveDirNorm = _wishDir;

            Vector3 forwardVelocity = Vector3.Cross (groundNormal, Quaternion.AngleAxis (-90, Vector3.up) * new Vector3 (surfer_moveData.velocity.x, 0f, surfer_moveData.velocity.z));

            // Set the target speed of the player
            float _wishSpeed = _wishDir.magnitude;
            _wishSpeed *= speed;

            // Accelerate
            float yVel = surfer_moveData.velocity.y;
            Accelerate (_wishDir, _wishSpeed, accel, false);

            float maxVelocityMagnitude = _config.maxVelocity;
            surfer_moveData.velocity = Vector3.ClampMagnitude (new Vector3 (surfer_moveData.velocity.x, 0f, surfer_moveData.velocity.z), maxVelocityMagnitude);
            surfer_moveData.velocity.y = yVel;

            float yVelStored = surfer_moveData.velocity.y;
            surfer_moveData.velocity.y = 0f;

            // Calculate how much slopes should affect movement
            float yVelocityNew = forwardVelocity.normalized.y * new Vector3 (surfer_moveData.velocity.x, 0f, surfer_moveData.velocity.z).magnitude;

            // Apply the Y-movement from slopes
            surfer_moveData.velocity.y = Mathf.Min (Mathf.Max (0f, yVelocityNew) + yVelStored, speed);

            // Jumping out of water
            bool movingForwards = playerTransform.InverseTransformVector (surfer_moveData.velocity).z > 0f;
            Trace waterJumpTrace = TraceBounds (playerTransform.position, playerTransform.position + playerTransform.forward * 0.1f, SurfPhysics.groundLayerMask);
            if (waterJumpTrace.hitCollider != null && Vector3.Angle (Vector3.up, waterJumpTrace.planeNormal) >= _config.slopeLimit && Input.GetButton ("Jump") && !surfer_moveData.cameraUnderwater && movingForwards)
                surfer_moveData.velocity.y = Mathf.Max (surfer_moveData.velocity.y, _config.jumpForce);

        }
        
        private void LadderCheck (Vector3 colliderScale, Vector3 direction) {
            var surfer_moveData = _surfer.XGet_moveData();
            if (surfer_moveData.velocity.sqrMagnitude <= 0f)
                return;
            
            bool foundLadder = false;

            RaycastHit [] hits = Physics.BoxCastAll (surfer_moveData.origin, Vector3.Scale (_surfer.XGet_collider().bounds.size * 0.5f, colliderScale), Vector3.Scale (direction, new Vector3 (1f, 0f, 1f)), Quaternion.identity, direction.magnitude, SurfPhysics.groundLayerMask, QueryTriggerInteraction.Collide);
            foreach (RaycastHit hit in hits) {

                Ladder ladder = hit.transform.GetComponentInParent<Ladder> ();
                if (ladder != null) {

                    bool allowClimb = true;
                    float ladderAngle = Vector3.Angle (Vector3.up, hit.normal);
                    if (surfer_moveData.angledLaddersEnabled) {

                        if (hit.normal.y < 0f)
                            allowClimb = false;
                        else {
                            
                            if (ladderAngle <= surfer_moveData.slopeLimit)
                                allowClimb = false;

                        }

                    } else if (hit.normal.y != 0f)
                        allowClimb = false;

                    if (allowClimb) {
                        foundLadder = true;
                        if (surfer_moveData.climbingLadder == false) {

                            surfer_moveData.climbingLadder = true;
                            surfer_moveData.ladderNormal = hit.normal;
                            surfer_moveData.ladderDirection = -hit.normal * direction.magnitude * 2f;

                            if (surfer_moveData.angledLaddersEnabled) {

                                Vector3 sideDir = hit.normal;
                                sideDir.y = 0f;
                                sideDir = Quaternion.AngleAxis (-90f, Vector3.up) * sideDir;

                                surfer_moveData.ladderClimbDir = Quaternion.AngleAxis (90f, sideDir) * hit.normal;
                                surfer_moveData.ladderClimbDir *= 1f/ surfer_moveData.ladderClimbDir.y; // Make sure Y is always 1

                            } else
                                surfer_moveData.ladderClimbDir = Vector3.up;
                            
                        }
                        
                    }

                }

            }

            if (!foundLadder) {
                
                surfer_moveData.ladderNormal = Vector3.zero;
                surfer_moveData.ladderVelocity = Vector3.zero;
                surfer_moveData.climbingLadder = false;
                surfer_moveData.ladderClimbDir = Vector3.up;

            }

        }

        private void LadderPhysics () {
            var surfer_moveData = _surfer.XGet_moveData();
            surfer_moveData.ladderVelocity = surfer_moveData.ladderClimbDir * surfer_moveData.verticalAxis * 6f;

            surfer_moveData.velocity = Vector3.Lerp (surfer_moveData.velocity, surfer_moveData.ladderVelocity, Time.deltaTime * 10f);

            LadderCheck (Vector3.one, surfer_moveData.ladderDirection);
            
            Trace floorTrace = TraceToFloor ();
            if (surfer_moveData.verticalAxis < 0f && floorTrace.hitCollider != null && Vector3.Angle (Vector3.up, floorTrace.planeNormal) <= surfer_moveData.slopeLimit) {

                surfer_moveData.velocity = surfer_moveData.ladderNormal * 0.5f;
                surfer_moveData.ladderVelocity = Vector3.zero;
                surfer_moveData.climbingLadder = false;

            }

            if (surfer_moveData.wishJump) {

                surfer_moveData.velocity = surfer_moveData.ladderNormal * 4f;
                surfer_moveData.ladderVelocity = Vector3.zero;
                surfer_moveData.climbingLadder = false;
                
            }
            
        }
        
        private void Accelerate (Vector3 wishDir, float wishSpeed, float acceleration, bool yMovement) {
            var surfer_moveData = _surfer.XGet_moveData();

            // Initialise variables
            float _addSpeed;
            float _accelerationSpeed;
            float _currentSpeed;
            
            // again, no idea
            _currentSpeed = Vector3.Dot (surfer_moveData.velocity, wishDir);
            _addSpeed = wishSpeed - _currentSpeed;

            // If you're not actually increasing your speed, stop here.
            if (_addSpeed <= 0)
                return;

            // won't bother trying to understand any of this, really
            _accelerationSpeed = Mathf.Min (acceleration * _deltaTime * wishSpeed, _addSpeed);

            // Add the velocity.
            var original = surfer_moveData.velocity;
            original.x += _accelerationSpeed * wishDir.x;
            if (yMovement)
            {
                original.y += _accelerationSpeed * wishDir.y;
            }
            original.z += _accelerationSpeed * wishDir.z;
            surfer_moveData.velocity = original;

        }

        private void ApplyFriction (float t, bool yAffected, bool grounded) {
            var surfer_moveData = _surfer.XGet_moveData();

            // Initialise variables
            Vector3 _vel = surfer_moveData.velocity;
            float _speed;
            float _newSpeed;
            float _control;
            float _drop;

            // Set Y to 0, speed to the magnitude of movement and drop to 0. I think drop is the amount of speed that is lost, but I just stole this from the internet, idk.
            _vel.y = 0.0f;
            _speed = _vel.magnitude;
            _drop = 0.0f;

            float fric = crouching ? _config.crouchFriction : _config.friction;
            float accel = crouching ? _config.crouchAcceleration : _config.acceleration;
            float decel = crouching ? _config.crouchDeceleration : _config.deceleration;

            // Only apply friction if the player is grounded
            if (grounded) {
                
                // i honestly have no idea what this does tbh
                _vel.y = surfer_moveData.velocity.y;
                _control = _speed < decel ? decel : _speed;
                _drop = _control * fric * _deltaTime * t;

            }

            // again, no idea, but comments look cool
            _newSpeed = Mathf.Max (_speed - _drop, 0f);
            if (_speed > 0.0f)
                _newSpeed /= _speed;

            // Set the end-velocity
            var original = surfer_moveData.velocity;
            original.x *= _newSpeed;
            if (yAffected == true) { original.y *= _newSpeed; }
            original.z *= _newSpeed;
            surfer_moveData.velocity = original;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private Vector3 AirInputMovement () {
            var surfer_moveData = _surfer.XGet_moveData();

            Vector3 wishVel, wishDir;
            float wishSpeed;

            GetWishValues (out wishVel, out wishDir, out wishSpeed);

            if (_config.clampAirSpeed && (wishSpeed != 0f && (wishSpeed > _config.maxSpeed))) {

                wishVel = wishVel * (_config.maxSpeed / wishSpeed);
                wishSpeed = _config.maxSpeed;

            }

            return SurfPhysics.AirAccelerate (surfer_moveData.velocity, wishDir, wishSpeed, _config.airAcceleration, _config.airCap, _deltaTime);

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="wishVel"></param>
        /// <param name="wishDir"></param>
        /// <param name="wishSpeed"></param>
        private void GetWishValues (out Vector3 wishVel, out Vector3 wishDir, out float wishSpeed) {
            var surfer_moveData = _surfer.XGet_moveData();

            wishVel = Vector3.zero;
            wishDir = Vector3.zero;
            wishSpeed = 0f;

            Vector3 forward = _surfer.XGet_forward(),
                right = _surfer.XGet_right();

            forward [1] = 0;
            right [1] = 0;
            forward.Normalize ();
            right.Normalize ();

            for (int i = 0; i < 3; i++)
                wishVel [i] = forward [i] * surfer_moveData.forwardMove + right [i] * surfer_moveData.sideMove;
            wishVel [1] = 0;

            wishSpeed = wishVel.magnitude;
            wishDir = wishVel.normalized;

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="velocity"></param>
        /// <param name="jumpPower"></param>
        private void Jump () {
            var surfer_moveData = _surfer.XGet_moveData();
            
            if (!_config.autoBhop)
                surfer_moveData.wishJump = false;
            
            // surfer_moveData.velocity.y += _config.jumpForce;
            surfer_moveData.velocity = V3YAdd(surfer_moveData.velocity, _config.jumpForce);
            jumping = true;

        }

        /// <summary>
        /// 
        /// </summary>
        private bool CheckGrounded () {
            var surfer_moveData = _surfer.XGet_moveData();

            surfer_moveData.surfaceFriction = 1f;
            var movingUp = surfer_moveData.velocity.y > 0f;
            var trace = TraceToFloor ();

            float groundSteepness = Vector3.Angle (Vector3.up, trace.planeNormal);

            if (trace.hitCollider == null || groundSteepness > _config.slopeLimit || (jumping && surfer_moveData.velocity.y > 0f)) {

                SetGround (null);

                // var xGetMoveType = _surfer.XGet_moveType();
                var xGetMoveType = MoveType.Walk; // Udon Stub: _surfer.XGet_moveType() raises a compiler error in U# 1.0b8
                if (movingUp && xGetMoveType != MoveType.Noclip)
                    surfer_moveData.surfaceFriction = _config.airFriction;
                
                return false;

            } else {

                groundNormal = trace.planeNormal;
                SetGround (trace.hitCollider.gameObject);
                return true;

            }

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj"></param>
        private void SetGround (GameObject obj) {
            var surfer_moveData = _surfer.XGet_moveData();

            if (obj != null) {

                _surfer.XSet_groundObject(obj);
                var original = surfer_moveData.velocity;
                original.y = 0;
                surfer_moveData.velocity = original;

            } else
                _surfer.XSet_groundObject(null);

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="layerMask"></param>
        /// <returns></returns>
        private Trace TraceBounds (Vector3 start, Vector3 end, int layerMask) {

            return Tracer.TraceCollider (traceHolder, _surfer.XGet_collider(), start, end, layerMask);

        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private Trace TraceToFloor () {
            var surfer_moveData = _surfer.XGet_moveData();

            var down = surfer_moveData.origin;
            down.y -= 0.15f;

            Debug.Log("Tracing to floor...");
            var traceToFloor = Tracer.TraceCollider (traceHolder, _surfer.XGet_collider(), surfer_moveData.origin, down, SurfPhysics.groundLayerMask);
            Debug.Log(_surfer.XGet_collider() + " " + surfer_moveData.origin + " " + down + " " + SurfPhysics.groundLayerMask);
            return traceToFloor;
        }

        public void Crouch (ISurfControllable surfer, MovementConfig config, float deltaTime)
        {
            _surfer = surfer;
            _config = config;
            _deltaTime = deltaTime;

            if (_surfer == null)
                return;

            var surfer_moveData = _surfer.XGet_moveData();
            var surfer_collider = _surfer.XGet_collider();

            if (surfer_collider == null)
                return;

            bool grounded = _surfer.XGet_groundObject() != null;
            bool wantsToCrouch = surfer_moveData.crouching;

            float crouchingHeight = Mathf.Clamp (surfer_moveData.crouchingHeight, 0.01f, 1f);
            float heightDifference = surfer_moveData.defaultHeight - surfer_moveData.defaultHeight * crouchingHeight;

            if (grounded)
                uncrouchDown = false;

            // Crouching input
            if (grounded)
                crouchLerp = Mathf.Lerp (crouchLerp, wantsToCrouch ? 1f : 0f, _deltaTime * surfer_moveData.crouchingSpeed);
            else if (!grounded && !wantsToCrouch && crouchLerp < 0.95f)
                crouchLerp = 0f;
            else if (!grounded && wantsToCrouch)
                crouchLerp = 1f;

            // Collider and position changing
            if (crouchLerp > 0.9f && !crouching) {

                // Begin crouching
                crouching = true;
                if (surfer_collider.GetType () == typeof (BoxCollider)) {

                    // Box collider
                    BoxCollider boxCollider = (BoxCollider)surfer_collider;
                    boxCollider.size = new Vector3 (boxCollider.size.x, surfer_moveData.defaultHeight * crouchingHeight, boxCollider.size.z);

                } else if (surfer_collider.GetType () == typeof (CapsuleCollider)) {

                    // Capsule collider
                    CapsuleCollider capsuleCollider = (CapsuleCollider)surfer_collider;
                    capsuleCollider.height = surfer_moveData.defaultHeight * crouchingHeight;

                }

                // Move position and stuff
                surfer_moveData.origin += heightDifference / 2 * (grounded ? Vector3.down : Vector3.up);
                foreach (Transform child in playerTransform) {

                    if (child == surfer_moveData.viewTransform)
                        continue;

                    child.localPosition = new Vector3 (child.localPosition.x, child.localPosition.y * crouchingHeight, child.localPosition.z);

                }

                uncrouchDown = !grounded;

            } else if (crouching) {

                // Check if the player can uncrouch
                bool canUncrouch = true;
                if (surfer_collider.GetType () == typeof (BoxCollider)) {

                    // Box collider
                    BoxCollider boxCollider = (BoxCollider)surfer_collider;
                    Vector3 halfExtents = boxCollider.size * 0.5f;
                    Vector3 startPos = boxCollider.transform.position;
                    Vector3 endPos = boxCollider.transform.position + (uncrouchDown ? Vector3.down : Vector3.up) * heightDifference;

                    Trace trace = Tracer.TraceBox (traceHolder, startPos, endPos, halfExtents, boxCollider.contactOffset, SurfPhysics.groundLayerMask);

                    if (trace.hitCollider != null)
                        canUncrouch = false;

                } else if (surfer_collider.GetType () == typeof (CapsuleCollider)) {

                    // Capsule collider
                    CapsuleCollider capsuleCollider = (CapsuleCollider)surfer_collider;
                    Vector3 point1 = capsuleCollider.center + Vector3.up * capsuleCollider.height * 0.5f;
                    Vector3 point2 = capsuleCollider.center + Vector3.down * capsuleCollider.height * 0.5f;
                    Vector3 startPos = capsuleCollider.transform.position;
                    Vector3 endPos = capsuleCollider.transform.position + (uncrouchDown ? Vector3.down : Vector3.up) * heightDifference;

                    Trace trace = Tracer.TraceCapsule (traceHolder, point1, point2, capsuleCollider.radius, startPos, endPos, capsuleCollider.contactOffset, SurfPhysics.groundLayerMask);

                    if (trace.hitCollider != null)
                        canUncrouch = false;

                }

                // Uncrouch
                if (canUncrouch && crouchLerp <= 0.9f) {

                    crouching = false;
                    if (surfer_collider.GetType () == typeof (BoxCollider)) {

                        // Box collider
                        BoxCollider boxCollider = (BoxCollider)surfer_collider;
                        boxCollider.size = new Vector3 (boxCollider.size.x, surfer_moveData.defaultHeight, boxCollider.size.z);

                    } else if (surfer_collider.GetType () == typeof (CapsuleCollider)) {

                        // Capsule collider
                        CapsuleCollider capsuleCollider = (CapsuleCollider)surfer_collider;
                        capsuleCollider.height = surfer_moveData.defaultHeight;

                    }

                    // Move position and stuff
                    surfer_moveData.origin += heightDifference / 2 * (uncrouchDown ? Vector3.down : Vector3.up);
                    foreach (Transform child in playerTransform) {

                        child.localPosition = new Vector3 (child.localPosition.x, child.localPosition.y / crouchingHeight, child.localPosition.z);

                    }

                }

                if (!canUncrouch)
                    crouchLerp = 1f;

            }

            // Changing camera position
            if (!crouching)
                surfer_moveData.viewTransform.localPosition = Vector3.Lerp (surfer_moveData.viewTransformDefaultLocalPos, surfer_moveData.viewTransformDefaultLocalPos * crouchingHeight + Vector3.down * heightDifference * 0.5f, crouchLerp);
            else
                surfer_moveData.viewTransform.localPosition = Vector3.Lerp (surfer_moveData.viewTransformDefaultLocalPos - Vector3.down * heightDifference * 0.5f, surfer_moveData.viewTransformDefaultLocalPos * crouchingHeight, crouchLerp);

        }

        void SlideMovement () {
            var surfer_moveData = _surfer.XGet_moveData();
            
            // Gradually change direction
            slideDirection += new Vector3 (groundNormal.x, 0f, groundNormal.z) * slideSpeedCurrent * _deltaTime;
            slideDirection = slideDirection.normalized;

            // Set direction
            Vector3 slideForward = Vector3.Cross (groundNormal, Quaternion.AngleAxis (-90, Vector3.up) * slideDirection);
            
            // Set the velocity
            slideSpeedCurrent -= _config.slideFriction * _deltaTime;
            slideSpeedCurrent = Mathf.Clamp (slideSpeedCurrent, 0f, _config.maximumSlideSpeed);
            slideSpeedCurrent -= (slideForward * slideSpeedCurrent).y * _deltaTime * _config.downhillSlideSpeedMultiplier; // Accelerate downhill (-y = downward, - * - = +)

            surfer_moveData.velocity = slideForward * slideSpeedCurrent;
            
            // Jump
            if (surfer_moveData.wishJump && slideSpeedCurrent < _config.minimumSlideSpeed * _config.slideSpeedMultiplier) {

                Jump ();
                return;

            }

        }

    }
}
