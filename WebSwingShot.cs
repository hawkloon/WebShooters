using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThunderRoad;
using System.Collections;
using UnityEngine;
using Sirenix.OdinInspector;

namespace WebShooters
{
    public class WebSwingShot : MonoBehaviour
    {
        public Item item;


        public WebShooterMono webShooterMono;


        public void InfoGather (WebShooterMono module)
        {
            item = GetComponent<Item>();
            webShooterMono = module;
            webShooterMono.SwingingLineRenderer.gameObject.SetActive(true);
            webShooterMono.activeLineTarget = item.transform.position;
            item.mainCollisionHandler.OnCollisionStartEvent += MainCollisionHandler_OnCollisionStartEvent;
            StartCoroutine(TimeOut());
        }
        private IEnumerator TimeOut()
        {
            yield return Yielders.ForSeconds(WebShooterMaster.WebTimeOut);
            EarlyDestroy();
        }
        private void MainCollisionHandler_OnCollisionStartEvent(CollisionInstance collisionInstance)
        {
            if (collisionInstance.targetCollider.GetComponentInParent<RagdollPart>() is RagdollPart part)
            {

                if (!part.ragdoll || !part.ragdoll.creature || part.ragdoll.creature.isPlayer) return;

                webShooterMono.SwingShotHit(collisionInstance, item, HitType.Creature, part.physicBody);
                return;
            }
            if (collisionInstance.targetCollider.GetComponentInParent<Item>()is Item i)
            {
                webShooterMono.SwingShotHit(collisionInstance, item, HitType.Item, i.physicBody);
                return;
            }

            webShooterMono.SwingShotHit(collisionInstance, item, HitType.Other);
            item.mainCollisionHandler.OnCollisionStartEvent -= MainCollisionHandler_OnCollisionStartEvent;
            
        }

        private void EarlyDestroy()
        {
            webShooterMono.lineShot = false;
            webShooterMono.SwingingLineRenderer.gameObject.SetActive(false);
            webShooterMono.activeLineTarget = null;
            webShooterMono.Activated = false;
            webShooterMono.canActivate = true;
            item.Despawn();
        }
        void Update()
        {
            if (!webShooterMono) return;
            webShooterMono.activeLineTarget = item.transform.position;
            if (item.waterHandler.inWater) EarlyDestroy();
        }
    }

    public enum HitType
    {
        Creature,
        Item,
        Other
    }
    public class WebController : MonoBehaviour
    {
        Item item;

        public GameObject coneIns;
       
        private WebCone webConeMono;
        WebShooterMono shooterMono;
        SpringJoint springJoint;
        private CollisionInstance collisionInstance;
        bool reelingIn;
        bool reelingOut;
        private HitType type;

        PhysicBody pBody;
        public void OnStart(HitType hitType, CollisionInstance instance, WebShooterMono webShooterMono, PhysicBody physicBody = null)
        {
            Debug.Log($"Web Controller Loaded");
            item = GetComponent<Item>();
            collisionInstance = instance;
            shooterMono = webShooterMono;
            reelingIn = false;
            reelingOut = false;
            shooterMono.wrist.SendMessage("HawkloonDisableTriggerAction");
            item.OnUngrabEvent += Item_OnUngrabEvent;
            item.mainCollisionHandler.OnTriggerEnterEvent += MainCollisionHandler_OnTriggerEnterEvent;
            shooterMono.lineShot = false;
            type = hitType;
            if (physicBody != null) pBody = physicBody;
            if (hitType == HitType.Other) Hit();


        }
        public void DestabilizeFromPhysicBody(PhysicBody body, GameObject game)
        {
            if (body == null) return;
            if(body.gameObject.GetComponent<RagdollPart>() is RagdollPart part)
            {
                if (part.ragdoll && part.ragdoll.creature && !part.ragdoll.creature.isKilled)
                {
                    part.ragdoll.SetState(Ragdoll.State.Destabilized);
                    part.ragdoll.creature.brain.AddNoStandUpModifier(shooterMono);
                    game.AddComponent<WebLineCleaner>().OnStart(part.ragdoll.creature);
                }
            }

            else if(body.gameObject.GetComponentInParent<RagdollPart>() is RagdollPart part2)
            {
                if (part2.ragdoll && part2.ragdoll.creature && !part2.ragdoll.creature.isKilled)
                {
                    part2.ragdoll.SetState(Ragdoll.State.Destabilized);
                    part2.ragdoll.creature.brain.AddNoStandUpModifier(shooterMono);
                    game.AddComponent<WebLineCleaner>().OnStart(part2.ragdoll.creature);
                }
            }
        }

        public void TryAddLineClean(Creature creature, GameObject game)
        {
            Debug.Log($"Attempting to add Line Clean");

            game.AddComponent<WebLineCleaner>().OnStart(creature);
        }
        public void CreateJoint(PhysicBody p1, PhysicBody p2, Vector3 pos, Vector3 pos2)
        {
            Debug.Log($"Creating Joint from tether");
            var game = new GameObject();
            game.transform.rotation = Quaternion.identity;
            var rb = game.AddComponent<Rigidbody>();

            var line = game.AddComponent<LineRenderer>();
            line.material = shooterMono.lineMaterial;
            line.startWidth = 0.05f;
            line.textureMode = LineTextureMode.Tile;
            line.endWidth = 0.05f;
            Debug.Log($"Base gameobject and line renderer set up");


           if (p1 && p2)
            {
                Debug.Log($"Both pBodies");
                game.transform.position = Vector3.Lerp(p1.transform.position, p2.transform.position, 0.5f);
                var joint = p2.gameObject.AddComponent<SpringJoint>();
                joint.connectedBody = p1.rigidBody;
                joint.autoConfigureConnectedAnchor = false;
                joint.connectedAnchor = Vector3.zero;
                joint.spring = 2500f;
                joint.damper = 150f;

                joint.maxDistance = Vector3.Distance(p2.transform.position, p1.transform.position) / 8f;
                game.AddComponent<FixedJoint>();
                game.transform.parent = p1.transform;
                var e = game.AddComponent<TetherUpdate>();
                CleanerAdd(p1, e);
                CleanerAdd(p2, e);
                e.start = p2.transform;
                e.end = p1.transform;
                DestabilizeFromPhysicBody(p1, game);
                DestabilizeFromPhysicBody(p2, game);
                return;
            }
            if (p1 && p2 == null)
            {
                game.transform.position = pos2;
                var joint = p1.gameObject.AddComponent<SpringJoint>();
                joint.connectedBody = rb;
                joint.autoConfigureConnectedAnchor = false;
                joint.connectedAnchor = Vector3.zero;
                joint.spring = 2500f;
                joint.damper = 150f;

                joint.maxDistance = Vector3.Distance(p1.transform.position, pos2) / 8f;
                game.AddComponent<FixedJoint>();
                var e = game.AddComponent<TetherUpdate>();
                CleanerAdd(p1,e);
                game.transform.parent = p1.transform;
                e.start = p1.transform;
                e.end = game.transform;
                DestabilizeFromPhysicBody(p1, game);
                Debug.Log($"pBody 1");
                return;
            }

            else if (p2 && p1 == null)
            {
                game.transform.position = pos;
                var joint = p2.gameObject.AddComponent<SpringJoint>();
                joint.connectedBody = rb;
                joint.autoConfigureConnectedAnchor = false;
                joint.connectedAnchor = Vector3.zero;
                joint.spring = 2500f;
                joint.damper = 150f;

                joint.maxDistance = Vector3.Distance(p2.transform.position, pos) / 8f;
                game.AddComponent<FixedJoint>();
                var e = game.AddComponent<TetherUpdate>();
                CleanerAdd(p2, e);
                game.transform.parent = p2.transform;
                e.start = p2.transform;
                e.end = game.transform;
                DestabilizeFromPhysicBody(p2, game);
                Debug.Log($"pBody 2");
                return;
            }



        }
        


        public void CleanerAdd(PhysicBody p, TetherUpdate tetherUpdate)
        {
            if(p.gameObject.transform.root.GetComponent<Creature>() is Creature c)
            {
                tetherUpdate.OnStart(c);
            }
        }
        public void Reset(RagdollHand hand, WebController webController2)
        {
            shooterMono.lineShot = false;
            webController2.shooterMono.lineShot = false;
            shooterMono.Activated = false;
            webController2.shooterMono.Activated = false;                       
            hand.UnGrab(false);
            hand.otherHand.UnGrab(false);
        }
        private void MainCollisionHandler_OnTriggerEnterEvent(Collider other)
        {
            if (shooterMono.getHand() == null) return;
            if (other.gameObject.GetComponentInParent<RagdollPart>() is RagdollPart part)
            {
                Debug.Log($"Web Controller entered a ragdoll part");
                if(part == item.mainHandler.otherHand)
                {
                    Debug.Log("Ragdoll part is other hand");
                    var hand = item.mainHandler.otherHand;
                    if (hand.grabbedHandle == null || hand.grabbedHandle.item == null || hand.grabbedHandle.item.itemId != item.itemId) return;
                    Debug.Log("Other hand is holding web controller");
                    if (shooterMono.lineMaterial == null) return;
                    Debug.Log("Line material isn't null");
                    if (hand.grabbedHandle.item.GetComponent<WebController>() == null) return; 
                    var webController2 = hand.grabbedHandle.item.GetComponent<WebController>();
                    if (pBody && webController2.pBody) Debug.Log($"Two PhysicBodies found");
                    if (pBody != null || webController2.pBody != null)
                    {
                        Debug.Log($"Physics body involved, making joint");
                        CreateJoint(pBody, webController2.pBody, collisionInstance.contactPoint, webController2.collisionInstance.contactPoint);
                        Reset(hand, webController2);
                    }
                    if (webConeMono == null || webController2.webConeMono == null || webController2.webConeMono.bone == null) return;
                    else if (webController2.type == HitType.Other && type == HitType.Other)
                    {
                        shooterMono.WebRopeCreate(webConeMono.bone.transform, webController2.webConeMono.bone.transform, shooterMono.lineMaterial);
                        Reset(hand, webController2);
                    }

                }
            }
        }

        private void Item_OnHeldActionEvent(RagdollHand ragdollHand, Handle handle, Interactable.Action action)
        {
            if(action == Interactable.Action.UseStart) reelingIn = true;
            if (action == Interactable.Action.UseStop) reelingIn = false;
            if (action == Interactable.Action.AlternateUseStart) reelingOut = true;
            if (action == Interactable.Action.AlternateUseStop) reelingOut = false;
        }   

        private SpringJoint JointConfig(Vector3 anchorPoint)
        {
            var spring = Player.local.locomotion.gameObject.AddComponent<SpringJoint>();
            spring.autoConfigureConnectedAnchor = false;
            spring.connectedAnchor = anchorPoint;
            spring.spring = 750f;
            spring.damper = 500f;


            spring.connectedMassScale = 10f;

            spring.minDistance = 0.1f;
            spring.maxDistance = Vector3.Distance(anchorPoint, shooterMono.nozzle.transform.position);


            return spring;
           
        }

        private void Item_OnUngrabEvent(Handle handle, RagdollHand ragdollHand, bool throwing)
        {
            Debug.Log($"Web Controller Ungrabbed!");
            pBody = null;
            if(springJoint) Destroy(springJoint); 
            var hand = shooterMono.getHand();
            if (hand.Velocity().magnitude > 5 && type == HitType.Other) Player.local.locomotion.rb.AddForce(-hand.Velocity() * WebShooterMaster.KickOffStrength * 10, ForceMode.Impulse);
            shooterMono.Activated = false;
            shooterMono.activeLineTarget = null;
            shooterMono.SwingingLineRenderer.gameObject.SetActive(false);
            shooterMono.StartCoroutine(shooterMono.HolderTriggerEnable(this));
            item.Despawn();
            item = null;
            Destroy(this);
        }

        public void Hit()
        {
            item.OnHeldActionEvent += Item_OnHeldActionEvent;
            coneIns = GameObject.Instantiate(shooterMono.conePrefab);
            coneIns.transform.position = collisionInstance.contactPoint;
            coneIns.transform.rotation = Quaternion.LookRotation(collisionInstance.contactNormal);
            webConeMono = coneIns.GetComponent<WebCone>();
            shooterMono.activeLineTarget = webConeMono.bone.transform.position;
            springJoint = JointConfig(collisionInstance.contactPoint);
        }
        public void ReelCheck()
        {
            if (!springJoint) return;
            if (reelingIn) springJoint.maxDistance -= Time.deltaTime * WebShooterMaster.ReelStrength;
            if (reelingOut) springJoint.maxDistance += Time.deltaTime * WebShooterMaster.ReelStrength;
        }

        public void ConeSet()
        {
            if (!coneIns || !webConeMono || !shooterMono) return;
            var lookDir = (shooterMono.nozzle.position - webConeMono.bone.transform.position).normalized;
            webConeMono.bone.transform.rotation = Quaternion.LookRotation(lookDir) * Quaternion.Euler(90, 0, 0);
        }

        public void VelocityCheck()
        {
            if (type == HitType.Other || pBody == null || (item.physicBody.velocity - Player.currentCreature.currentLocomotion.rb.velocity).magnitude < 7f) return;
            Debug.Log($"Velocity Check Check 1");
            if (type == HitType.Creature)
            {
               if(pBody.gameObject.GetComponent<RagdollPart>() is RagdollPart part ) part.ragdoll.SetState(Ragdoll.State.Destabilized);
            }
            if (pBody.gameObject.GetComponent<Breakable>() is Breakable breakable && pBody.isKinematic)
            {
                breakable.Break();
                foreach(PhysicBody physicBody in breakable.unbrokenObjectsHolder.GetComponentsInChildren<PhysicBody>())
                {
                    physicBody.AddForce(item.physicBody.velocity.normalized * 30, ForceMode.Impulse);
                }
                Debug.Log($"Velocity Check Breakable");
            }
            else
            {
                Debug.Log($"Velocity Check non-breakable");
                pBody.AddForce(item.physicBody.velocity.normalized * (type == HitType.Creature ? 5 : 3) * 10, ForceMode.Impulse);
            }
        }

        public void Update()
        {
            ReelCheck();
            ConeSet();
            VelocityCheck();
            if (pBody) shooterMono.activeLineTarget = pBody.transform.position;
        }
    }





    public class TetherUpdate : MonoBehaviour
    {
        public Transform start;
        public Transform end;

        public LineRenderer line;

        public List<Creature> creature;

        public void OnStart(Creature c)
        {
            if(creature == null) creature = new List<Creature>();
            creature.Add(c);
            Debug.Log($"Creature reference added to cleaner");
        }

        public void Start()
        {
            line = GetComponent<LineRenderer>();
            if (creature == null) return;
            foreach (Creature cr in creature)
            {
                cr.OnDespawnEvent += C_OnDespawnEvent;
            }
            Debug.Log($"Cleaner Staarted");
        }

        private void C_OnDespawnEvent(EventTime eventTime)
        {
            Debug.Log($"Creature Despawned, cleaning tether");
            if (!gameObject) return;
            Destroy(gameObject);
            Destroy(this);
        }

        void Update()
        {
            if (!line || !start || !end) return;
            line.SetPosition(0, start.position);
            line.SetPosition(1, end.position);
        }
    }

    public class WebLineCleaner : MonoBehaviour
    {
        Creature creature;

        public void OnStart(Creature c)
        {
            creature = c;
            c.OnDespawnEvent += C_OnDespawnEvent;
            Debug.Log($"Line Cleaner added to tether");
        }

        private void C_OnDespawnEvent(EventTime eventTime)
        {
            if (eventTime == EventTime.OnEnd)
            {
                Debug.Log($"Cleaning up web tether");
                Destroy(this);
            }
         }
    }
}
