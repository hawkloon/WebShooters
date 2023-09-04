using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThunderRoad;
using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using TotT;

namespace WebShooters
{
    public class WebShooterModule : ItemModule
    {
        public List<WebShooterMode> WebShooterModes;

        public bool UIAllowed;

        public bool LaserSightAllowed;

        public override void OnItemLoaded(Item item)
        {
            base.OnItemLoaded(item);
            item.gameObject.AddComponent<WebShooterMono>();
        }
    }
    public static class Extensions
    {
        public static Transform TryFind(this Transform parent, string name)
        {
            foreach (Transform child in parent.GetComponentsInChildren<Transform>(true))
            {
                Debug.Log(child.name);
                if (string.CompareOrdinal(name, child.name) == 0)
                {
                    return child;
                }
            }

            return null;
        }
        public static void Set<T>(this object source, string fieldName, T val)
        {
            source.GetType()
                .GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(source, val);
        }
        public static float randomAxisLength(float original, float min, float max)
        {
            var random = Random.Range(min, max);

            return original * random;
        }

        public static void CalculateRandomScale(GameObject game, bool isCreature, float min, float max)
        {
            var randomScale = new Vector3(randomAxisLength(game.transform.localScale.x, min, max), randomAxisLength(game.transform.localScale.y, min, max), game.transform.localScale.z);
            game.transform.localScale = randomScale;
            if (isCreature) game.transform.localScale /= 2;
        }

        public static Creature GetCreatureFromRaycast(RaycastHit hit)
        {
            return hit.collider.transform.root.GetComponent<Creature>();

        }
    }
    public class WebShooterMono : ArmModule
    {
        public bool firstTap;
        public bool Timer;
        public static float tapTimerMax = 0.15f;
        public float tapTimer = tapTimerMax;

        public Transform nozzle;
        public Vector3? activeLineTarget;
        public SpringJoint activeSwingJoint;
        public Material lineMaterial;

        public Dictionary<string, AudioSource> sounds = new Dictionary<string, AudioSource>();
        public LineRenderer SwingingLineRenderer;

        public GameObject webGameObject;
        public GameObject conePrefab;
        public GameObject WebLinePrefab;
        public WebSelection webSelection;
        public List<WebShooterMode> webShooterModes;
        public WebShooterModule module;
        public WebShooterMode currentMode;

        private GameObject laserSight;
        public bool useLaserSight = true;

        public bool lineShot;
        public bool isSwinging;

        public Holder wrist;
        public override void OnStart()
        {
            base.OnStart();
            HasAltMode = true;
            useDeactivate = false;
            HasCreatureActivate = false;
            firstTap = false;
            Debug.Log($"{WebShooterMaster.Nozzle} fuck");
            PointerInputModule.SetUICameraToAllCanvas();
            Timer = false;
            nozzle = item.GetCustomReference("Nozzle");
            item.TryGetCustomReference<WebSelection>("WebSelection", out webSelection);
            item.TryGetCustomReference<Transform>("LaserSight", out Transform laser);
            if (laser) laserSight = laser.gameObject;
            lineShot = false;
            var data = item.data.GetModule<WebShooterModule>();
            module = data;
            if (data.UIAllowed)
            {
                WebModesSetUp(data);
            }
            if (data.LaserSightAllowed)
            {
                useLaserSight = true;
                if (Physics.Raycast(nozzle.position, nozzle.forward, out RaycastHit hit, 205f, -1, QueryTriggerInteraction.Ignore))
                {
                    laserSight.SetActive(true);
                    laserSight.transform.position = hit.point;
                    laserSight.transform.rotation = Quaternion.LookRotation(hit.normal);
                }
                else laserSight.SetActive(false);
            }
            else
            {
                useLaserSight = false;
            }
            Catalog.LoadAssetAsync<GameObject>("WebShooterWeb", callback => { webGameObject = callback; }, "Web");
            Catalog.LoadAssetAsync<GameObject>("WebCone", cone => { conePrefab = cone; } , "Cone");
            Catalog.LoadAssetAsync<GameObject>("WebLinePrefab", line => { WebLinePrefab = line; }, "Line");
            foreach(AudioSource source in item.GetCustomReference("Sounds").GetComponentsInChildren<AudioSource>())
            {
                sounds.Add(source.gameObject.name, source);
                Debug.Log($"{source.gameObject.name}");
            }
            SwingingLineRenderer = LineSetUp();
        }

        public IEnumerator HolderTriggerEnable(WebController controller)
        {
            yield return Yielders.ForSeconds(0.5f);
            wrist.SendMessage("HawkloonEnableTriggerAction");
            if (controller) DestroyImmediate(controller);
        }
        public override void AltMode()
        {
            base.AltMode();
            if(module.UIAllowed && webSelection != null)
            {
                Debug.Log("Web shooter model accepts for UI");
                if (!webSelection.transform.parent.gameObject.activeInHierarchy)
                {
                    webSelection.transform.parent.gameObject.SetActive(true);
                    Player.currentCreature.animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
                    Pointer.activeSide = getHand().otherHand.side;
                }
                else
                {
                    webSelection.transform.parent.gameObject.SetActive(false);
                    if(WebShooterMaster.DefaultSide != null) Pointer.activeSide = (Side) WebShooterMaster.DefaultSide;
                }
            }
        }

        public void WebModesSetUp(WebShooterModule module)
        {
            currentMode = null;
            webShooterModes = item.data.GetModule<WebShooterModule>().WebShooterModes;
            SwingingLineRenderer = LineSetUp();
            if (webShooterModes == null || webShooterModes.Count == 0) return;
            for(int i = 0; i < webShooterModes.Count; i++)
            {
                WebModeButtonSetUp(webShooterModes[i], i);
            }
            webSelection.normalWebs.onClick.AddListener(ModeReset);
            webSelection.transform.parent.gameObject.SetActive(false);
        }

        public void ModeReset()
        {
            currentMode = null;
        }

        public void WebModeButtonSetUp(WebShooterMode effect, int i)
        {
            var mode = webShooterModes[i];
            var game = GameObject.Instantiate(webSelection.TemplateButton, webSelection.anchorPoints[i + 1]);
            game.SetActive(true);
            game.name = mode.Name;
            game.transform.localPosition = Vector3.zero;
            game.transform.localEulerAngles = new Vector3(0f, 0f, 90f);
            var icon = game.GetComponent<Image>();
            Catalog.LoadAssetAsync<Sprite>(mode.IconID, callback => { icon.sprite = callback; }, mode.IconID);
            icon.color = icon.color;
            var button = game.AddComponent<WebModeButton>();
            button.webSelection = webSelection;
            button.shooterMode = mode;
            button.acitveMono = this;
        }

        public override void On()
        {
            base.On();

        }

        public override void Off()
        {
            base.Off();
            Debug.Log($"Web Shooter deactivated on hand {getHand().side}");
        }

        public override void OnSnapEvent(Holder holder)
        {
            base.OnSnapEvent(holder);
            if(getMount() != null)
            {
                wrist = getMount().item.holder;

            }
            else
            {
                wrist = item.holder;
            }

            if (!WebShooterMaster.Nozzle)
            {
                if (nozzle.transform.GetComponentInParent<RagdollHand>()is RagdollHand h)
                {
                    if(h.side != getHand().side)
                    {
                        nozzle = Extensions.TryFind(getHand().transform, "WebDirection");

                        if (nozzle == null)
                        {
                            var game = new GameObject("WebDirection");
                            game.transform.parent = getHand().transform;

                            game.transform.localPosition = Vector3.zero;
                            game.transform.localEulerAngles = new Vector3(0f, -90f, 0f);
                            nozzle = game.transform;
                        }
                    }
                }
            }
            Debug.Log($"Holder = {wrist.gameObject.name}");
        }

        public void SwingShotHit(CollisionInstance instance, Item shot, HitType hitType, PhysicBody physicBody = null)
        {
            var hand = getHand();
            shot.Despawn();
            if (hand.grabbedHandle != null) return;
            Catalog.GetData<ItemData>("WebController").SpawnAsync(controller =>
            {
                controller.transform.position = hand.transform.position;
                controller.transform.rotation = Quaternion.identity;
                hand.Grab(controller.GetMainHandle(hand.side), true);

                if (controller.gameObject.GetComponent<WebController>()) return;
                controller.gameObject.AddComponent<WebController>().OnStart(hitType, instance, this, physicBody);


            });
        } 
        public LineRenderer LineSetUp()
        {
            var game = new GameObject("WebLine");
            game.transform.parent = item.transform;
            game.transform.localPosition = Vector3.zero;
            game.transform.localEulerAngles = Vector3.zero;

            var line = game.AddComponent<LineRenderer>();
            Catalog.LoadAssetAsync<Material>("WebLineMaterial", mat =>
            {
                line.material = mat;
                lineMaterial = mat;
            }, "WebLineMaterial");
            line.startWidth = 0.05f;
            line.textureMode = LineTextureMode.Tile;
            line.endWidth = 0.05f;

            line.gameObject.SetActive(false);

            return line;
        }


        public void PlaySound(string soundName)
        {
            foreach(KeyValuePair<string, AudioSource> kvp in sounds)
            {
                if(kvp.Key == soundName)
                {
                    kvp.Value.Play();
                    return;
                }
            }
        }

        public override void Activate()
        {
            base.Activate();
            if (!firstTap)
            {
                firstTap = true;
                Timer = true;
                Activated = true;
                Cooldown(0.2f);
            }
            Debug.Log($"Activate Registered");
        }

        public void Swing()
        {
            PlaySound("Swing");
            Debug.Log($"Web Swing");
            Catalog.GetData<ItemData>("WebShot").SpawnAsync(web =>
            {
                lineShot = true;
                if(WebShooterMaster.WebGravity < 1) web.mainCollisionHandler.SetPhysicModifier(this, WebShooterMaster.WebGravity);
                web.physicBody.AddForce(nozzle.transform.forward * WebShooterMaster.WebLineForce, ForceMode.Impulse);
                web.Throw();
                web.ignoredItem = item;
                web.ignoredRagdoll = Player.local.creature.ragdoll;
                web.gameObject.AddComponent<WebSwingShot>().InfoGather(this);
            }, nozzle.transform.position + (nozzle.forward / 3), nozzle.transform.rotation);
        }

        public void Hold()
        {
            if (lineShot) return;
            Swing();
        }

        void CreatureHit(RagdollPart part, GameObject plane)
        {
            if (part.ragdoll == null || part.ragdoll.creature == null) return;
            var c = part.ragdoll.creature;
            if (c.ragdoll.state == Ragdoll.State.NoPhysic) c.ragdoll.AddPhysicToggleModifier(this);
            c.TryPush(Creature.PushType.Hit, (transform.forward) * 40, 2, part.type);
            plane.transform.parent = part.transform;
        }
        void ShootWeb()
        {
            PlaySound("Swing");
            Debug.Log($"Web Shot");
            Catalog.GetData<ItemData>("WebShot").SpawnAsync(web =>
            {
                if (WebShooterMaster.WebGravity < 1) web.mainCollisionHandler.SetPhysicModifier(this, WebShooterMaster.WebGravity);
                web.physicBody.AddForce(nozzle.transform.forward * WebShooterMaster.WebLineForce, ForceMode.Impulse);
                web.Throw();
                web.ignoredItem = item;
                web.ignoredRagdoll = Player.local.creature.ragdoll;
                web.gameObject.AddComponent<WebShot>().OnStart(this);
            }, nozzle.transform.position + (nozzle.forward / 3), nozzle.transform.rotation);
        }
    

        public void Tap()
        {
            if (currentMode != null && !currentMode.ShootWeb)
            {
                currentMode.OnActivate(this);
                Activated = false;
                return;
            }

            ShootWeb();
        }
        public void TapOrHold()
        {
            var Hand = getHand();
            if (Hand == null) return;
            if (!Timer) return;
            if (tapTimer >= 0)
            {
                tapTimer -= Time.deltaTime;
            }
            else
            {
                Timer = false;
                tapTimer = tapTimerMax;

                if (Hand.playerHand.controlHand.usePressed && Hand.playerHand.controlHand.gripPressed)
                {
                    Debug.Log($"Hold Func");
                    Hold();
                }
                else
                {
                    Debug.Log($"Tap Func");
                    Tap();
                }
                firstTap = false;
            }
            Activated = false;
        }


        public void LineRendererUpdate()
        {
            if (!canActivate) return;
            if(!SwingingLineRenderer.gameObject.activeInHierarchy && activeSwingJoint)
            {
                Destroy(activeSwingJoint);
            }
            if(SwingingLineRenderer.gameObject.activeInHierarchy && activeLineTarget != null)
            {
                SwingingLineRenderer.SetPosition(0, nozzle.position);
                SwingingLineRenderer.SetPosition(1, (Vector3)activeLineTarget);
            }
        }


        public void HandPoseForce()
        {
            var Hand = getHand();
            Hand.caster.DisallowCasting(this);
            Hand.caster.DisableSpellWheel(this);

            var pose = Catalog.GetData<HandPoseData>("WebShootingPose");
            Hand.poser.SetDefaultPose(pose);
            Hand.poser.SetTargetPose(pose);
            Hand.poser.UpdatePoseIndex(1f);
            Hand.poser.UpdatePoseLittle(1f);
            Hand.poser.UpdatePoseMiddle(1f);
            Hand.poser.UpdatePoseRing(1f);
            Hand.poser.UpdatePoseThumb(1f);
        }

        public void WebRopeCreate(Transform start, Transform target, Material material)
        {

            var webLine = GameObject.Instantiate(WebLinePrefab);
            webLine.transform.position = Vector3.Lerp(start.position, target.position, 0.5f);
            webLine.transform.rotation = Utils.LookRotation(target.position - start.position, UnityEngine.Vector3.up, UnityEngine.Vector3.up);
            var handle = webLine.GetComponent<Handle>();
            handle.axisLength = Vector3.Distance(start.position, target.position);
            handle.handOverlapColliders = new List<Collider>();
            handle.touchCollider = handle.gameObject.AddComponent<CapsuleCollider>();
            (handle.touchCollider as CapsuleCollider).radius = handle.touchRadius;
            (handle.touchCollider as CapsuleCollider).center = handle.touchCenter;
            (handle.touchCollider as CapsuleCollider).height = handle.axisLength;
            (handle.touchCollider as CapsuleCollider).direction = 1;
            var data = (HandleData)Catalog.GetData<InteractableData>("ObjectHandleLight");
            data.allowTelekinesis = false;
            data.highlightDefaultTitle = "Web Line";
            handle.Load(data);
            var line = webLine.AddComponent<LineRenderer>();
            line.material = material;
            line.startWidth = 0.05f;
            line.textureMode = LineTextureMode.Tile;
            line.endWidth = 0.05f;
            line.SetPosition(0, start.position);
            line.SetPosition(1, target.position);
        }

        public void HandPoseReset()
        {
            var Hand = getHand();
            Hand.caster.AllowCasting(this);
            Hand.caster.AllowSpellWheel(this);

            Hand.poser.ResetDefaultPose();
            Hand.poser.ResetTargetPose();
        }

        public void LaserSightUpdate()
        {
            if (Physics.Raycast(nozzle.position + (nozzle.forward /2), nozzle.forward, out RaycastHit hit, 205f, -1, QueryTriggerInteraction.Ignore))
            {
                if (hit.collider.GetComponentInParent<Player>()) return;
                laserSight.SetActive(true);
                laserSight.transform.position = hit.point;
                laserSight.transform.rotation = Quaternion.LookRotation(hit.normal);
            }           
        }


        public void ModOptionCheck()
        {
            if (!nozzle) return;
            
            if(getHand() == null) return;
            if(WebShooterMaster.Nozzle && nozzle.transform.parent != item.transform)
            {
                item.TryGetCustomReference("Nozzle", out nozzle);
                if (nozzle == null) nozzle = item.flyDirRef;
            }

            else if(!WebShooterMaster.Nozzle && nozzle.transform.parent == item.transform)
            {
                nozzle = Extensions.TryFind(getHand().transform, "WebDirection");

                if(nozzle == null)
                {
                    var game = new GameObject("WebDirection");
                    game.transform.parent = getHand().transform;

                    game.transform.localPosition = Vector3.zero;
                    game.transform.localEulerAngles = new Vector3(0f, -90f, 0f);
                    nozzle = game.transform;
                }
            }
        }

        public override void OnUpdate()
        {
            base.OnUpdate();

            TapOrHold();

            LineRendererUpdate();

            ModOptionCheck();

            if (useLaserSight && laserSight != null) LaserSightUpdate();
        }
    }
}
