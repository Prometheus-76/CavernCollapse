// GENERATED AUTOMATICALLY FROM 'Assets/Input/InputMaster.inputactions'

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;

public class @InputMaster : IInputActionCollection, IDisposable
{
    public InputActionAsset asset { get; }
    public @InputMaster()
    {
        asset = InputActionAsset.FromJson(@"{
    ""name"": ""InputMaster"",
    ""maps"": [
        {
            ""name"": ""Editor"",
            ""id"": ""25c8009d-981e-4642-8999-16435d5ea7ae"",
            ""actions"": [
                {
                    ""name"": ""Modifier"",
                    ""type"": ""Button"",
                    ""id"": ""ccc5c2f8-6b93-4e9c-b9ce-ad1125d2d073"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """"
                },
                {
                    ""name"": ""Previous"",
                    ""type"": ""Button"",
                    ""id"": ""3496550f-482c-4196-bfae-b603543d5e8a"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """"
                },
                {
                    ""name"": ""Next"",
                    ""type"": ""Button"",
                    ""id"": ""5007c8d2-69b2-46d0-b951-6b0ee9d2c10a"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """"
                },
                {
                    ""name"": ""Save"",
                    ""type"": ""Button"",
                    ""id"": ""2ef2698d-1b0d-4635-8430-17e4467de5c6"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """"
                },
                {
                    ""name"": ""Undo"",
                    ""type"": ""Button"",
                    ""id"": ""192f7f3d-5966-44dd-bfa1-88f90199bdfd"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """"
                },
                {
                    ""name"": ""Redo"",
                    ""type"": ""Button"",
                    ""id"": ""48da11a7-8c14-4245-9df2-7092dc1c4659"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """"
                },
                {
                    ""name"": ""Cursor"",
                    ""type"": ""Value"",
                    ""id"": ""12253991-6857-48dd-bc3b-699b26f8b28b"",
                    ""expectedControlType"": ""Vector2"",
                    ""processors"": """",
                    ""interactions"": """"
                },
                {
                    ""name"": ""Place"",
                    ""type"": ""Button"",
                    ""id"": ""479f1a7d-13fd-40b2-bcdb-a4f68abb1a81"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """"
                },
                {
                    ""name"": ""Remove"",
                    ""type"": ""Button"",
                    ""id"": ""40712398-1419-4399-bc19-bdcc62686d97"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """"
                }
            ],
            ""bindings"": [
                {
                    ""name"": """",
                    ""id"": ""c438684e-6eb6-49a2-9436-2e471e81dcfc"",
                    ""path"": ""<Keyboard>/ctrl"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""KBM"",
                    ""action"": ""Modifier"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""e2cacb1d-85a0-443d-bd92-f94e485e3e94"",
                    ""path"": ""<Keyboard>/a"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""KBM"",
                    ""action"": ""Previous"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""50653e8a-8770-472a-9342-9bd8b7b4da28"",
                    ""path"": ""<Keyboard>/d"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""KBM"",
                    ""action"": ""Next"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""386bc5a5-0044-4f23-9c48-057483778048"",
                    ""path"": ""<Keyboard>/s"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""KBM"",
                    ""action"": ""Save"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""5ea895d6-8937-47d1-991c-6812a94c7515"",
                    ""path"": ""<Keyboard>/z"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""KBM"",
                    ""action"": ""Undo"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""116a2c61-efe2-4361-a9b3-42fab484ac2a"",
                    ""path"": ""<Keyboard>/y"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""KBM"",
                    ""action"": ""Redo"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""84be2759-701a-4bf1-9162-f1366e1a381f"",
                    ""path"": ""<Pointer>/position"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""KBM"",
                    ""action"": ""Cursor"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""1ef6a4cb-eed7-4327-badc-ccc6c2de402b"",
                    ""path"": ""<Mouse>/leftButton"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""KBM"",
                    ""action"": ""Place"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""c5d274bd-9568-4233-9fc0-e8e3ca95e2d1"",
                    ""path"": ""<Mouse>/rightButton"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""KBM"",
                    ""action"": ""Remove"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                }
            ]
        }
    ],
    ""controlSchemes"": [
        {
            ""name"": ""KBM"",
            ""bindingGroup"": ""KBM"",
            ""devices"": [
                {
                    ""devicePath"": ""<Keyboard>"",
                    ""isOptional"": false,
                    ""isOR"": false
                },
                {
                    ""devicePath"": ""<Mouse>"",
                    ""isOptional"": false,
                    ""isOR"": false
                }
            ]
        }
    ]
}");
        // Editor
        m_Editor = asset.FindActionMap("Editor", throwIfNotFound: true);
        m_Editor_Modifier = m_Editor.FindAction("Modifier", throwIfNotFound: true);
        m_Editor_Previous = m_Editor.FindAction("Previous", throwIfNotFound: true);
        m_Editor_Next = m_Editor.FindAction("Next", throwIfNotFound: true);
        m_Editor_Save = m_Editor.FindAction("Save", throwIfNotFound: true);
        m_Editor_Undo = m_Editor.FindAction("Undo", throwIfNotFound: true);
        m_Editor_Redo = m_Editor.FindAction("Redo", throwIfNotFound: true);
        m_Editor_Cursor = m_Editor.FindAction("Cursor", throwIfNotFound: true);
        m_Editor_Place = m_Editor.FindAction("Place", throwIfNotFound: true);
        m_Editor_Remove = m_Editor.FindAction("Remove", throwIfNotFound: true);
    }

    public void Dispose()
    {
        UnityEngine.Object.Destroy(asset);
    }

    public InputBinding? bindingMask
    {
        get => asset.bindingMask;
        set => asset.bindingMask = value;
    }

    public ReadOnlyArray<InputDevice>? devices
    {
        get => asset.devices;
        set => asset.devices = value;
    }

    public ReadOnlyArray<InputControlScheme> controlSchemes => asset.controlSchemes;

    public bool Contains(InputAction action)
    {
        return asset.Contains(action);
    }

    public IEnumerator<InputAction> GetEnumerator()
    {
        return asset.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public void Enable()
    {
        asset.Enable();
    }

    public void Disable()
    {
        asset.Disable();
    }

    // Editor
    private readonly InputActionMap m_Editor;
    private IEditorActions m_EditorActionsCallbackInterface;
    private readonly InputAction m_Editor_Modifier;
    private readonly InputAction m_Editor_Previous;
    private readonly InputAction m_Editor_Next;
    private readonly InputAction m_Editor_Save;
    private readonly InputAction m_Editor_Undo;
    private readonly InputAction m_Editor_Redo;
    private readonly InputAction m_Editor_Cursor;
    private readonly InputAction m_Editor_Place;
    private readonly InputAction m_Editor_Remove;
    public struct EditorActions
    {
        private @InputMaster m_Wrapper;
        public EditorActions(@InputMaster wrapper) { m_Wrapper = wrapper; }
        public InputAction @Modifier => m_Wrapper.m_Editor_Modifier;
        public InputAction @Previous => m_Wrapper.m_Editor_Previous;
        public InputAction @Next => m_Wrapper.m_Editor_Next;
        public InputAction @Save => m_Wrapper.m_Editor_Save;
        public InputAction @Undo => m_Wrapper.m_Editor_Undo;
        public InputAction @Redo => m_Wrapper.m_Editor_Redo;
        public InputAction @Cursor => m_Wrapper.m_Editor_Cursor;
        public InputAction @Place => m_Wrapper.m_Editor_Place;
        public InputAction @Remove => m_Wrapper.m_Editor_Remove;
        public InputActionMap Get() { return m_Wrapper.m_Editor; }
        public void Enable() { Get().Enable(); }
        public void Disable() { Get().Disable(); }
        public bool enabled => Get().enabled;
        public static implicit operator InputActionMap(EditorActions set) { return set.Get(); }
        public void SetCallbacks(IEditorActions instance)
        {
            if (m_Wrapper.m_EditorActionsCallbackInterface != null)
            {
                @Modifier.started -= m_Wrapper.m_EditorActionsCallbackInterface.OnModifier;
                @Modifier.performed -= m_Wrapper.m_EditorActionsCallbackInterface.OnModifier;
                @Modifier.canceled -= m_Wrapper.m_EditorActionsCallbackInterface.OnModifier;
                @Previous.started -= m_Wrapper.m_EditorActionsCallbackInterface.OnPrevious;
                @Previous.performed -= m_Wrapper.m_EditorActionsCallbackInterface.OnPrevious;
                @Previous.canceled -= m_Wrapper.m_EditorActionsCallbackInterface.OnPrevious;
                @Next.started -= m_Wrapper.m_EditorActionsCallbackInterface.OnNext;
                @Next.performed -= m_Wrapper.m_EditorActionsCallbackInterface.OnNext;
                @Next.canceled -= m_Wrapper.m_EditorActionsCallbackInterface.OnNext;
                @Save.started -= m_Wrapper.m_EditorActionsCallbackInterface.OnSave;
                @Save.performed -= m_Wrapper.m_EditorActionsCallbackInterface.OnSave;
                @Save.canceled -= m_Wrapper.m_EditorActionsCallbackInterface.OnSave;
                @Undo.started -= m_Wrapper.m_EditorActionsCallbackInterface.OnUndo;
                @Undo.performed -= m_Wrapper.m_EditorActionsCallbackInterface.OnUndo;
                @Undo.canceled -= m_Wrapper.m_EditorActionsCallbackInterface.OnUndo;
                @Redo.started -= m_Wrapper.m_EditorActionsCallbackInterface.OnRedo;
                @Redo.performed -= m_Wrapper.m_EditorActionsCallbackInterface.OnRedo;
                @Redo.canceled -= m_Wrapper.m_EditorActionsCallbackInterface.OnRedo;
                @Cursor.started -= m_Wrapper.m_EditorActionsCallbackInterface.OnCursor;
                @Cursor.performed -= m_Wrapper.m_EditorActionsCallbackInterface.OnCursor;
                @Cursor.canceled -= m_Wrapper.m_EditorActionsCallbackInterface.OnCursor;
                @Place.started -= m_Wrapper.m_EditorActionsCallbackInterface.OnPlace;
                @Place.performed -= m_Wrapper.m_EditorActionsCallbackInterface.OnPlace;
                @Place.canceled -= m_Wrapper.m_EditorActionsCallbackInterface.OnPlace;
                @Remove.started -= m_Wrapper.m_EditorActionsCallbackInterface.OnRemove;
                @Remove.performed -= m_Wrapper.m_EditorActionsCallbackInterface.OnRemove;
                @Remove.canceled -= m_Wrapper.m_EditorActionsCallbackInterface.OnRemove;
            }
            m_Wrapper.m_EditorActionsCallbackInterface = instance;
            if (instance != null)
            {
                @Modifier.started += instance.OnModifier;
                @Modifier.performed += instance.OnModifier;
                @Modifier.canceled += instance.OnModifier;
                @Previous.started += instance.OnPrevious;
                @Previous.performed += instance.OnPrevious;
                @Previous.canceled += instance.OnPrevious;
                @Next.started += instance.OnNext;
                @Next.performed += instance.OnNext;
                @Next.canceled += instance.OnNext;
                @Save.started += instance.OnSave;
                @Save.performed += instance.OnSave;
                @Save.canceled += instance.OnSave;
                @Undo.started += instance.OnUndo;
                @Undo.performed += instance.OnUndo;
                @Undo.canceled += instance.OnUndo;
                @Redo.started += instance.OnRedo;
                @Redo.performed += instance.OnRedo;
                @Redo.canceled += instance.OnRedo;
                @Cursor.started += instance.OnCursor;
                @Cursor.performed += instance.OnCursor;
                @Cursor.canceled += instance.OnCursor;
                @Place.started += instance.OnPlace;
                @Place.performed += instance.OnPlace;
                @Place.canceled += instance.OnPlace;
                @Remove.started += instance.OnRemove;
                @Remove.performed += instance.OnRemove;
                @Remove.canceled += instance.OnRemove;
            }
        }
    }
    public EditorActions @Editor => new EditorActions(this);
    private int m_KBMSchemeIndex = -1;
    public InputControlScheme KBMScheme
    {
        get
        {
            if (m_KBMSchemeIndex == -1) m_KBMSchemeIndex = asset.FindControlSchemeIndex("KBM");
            return asset.controlSchemes[m_KBMSchemeIndex];
        }
    }
    public interface IEditorActions
    {
        void OnModifier(InputAction.CallbackContext context);
        void OnPrevious(InputAction.CallbackContext context);
        void OnNext(InputAction.CallbackContext context);
        void OnSave(InputAction.CallbackContext context);
        void OnUndo(InputAction.CallbackContext context);
        void OnRedo(InputAction.CallbackContext context);
        void OnCursor(InputAction.CallbackContext context);
        void OnPlace(InputAction.CallbackContext context);
        void OnRemove(InputAction.CallbackContext context);
    }
}
