using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace AuxiliaryComponents
{
    /// <summary>
    /// Component that manages button interactability based on string IDs.
    /// When any lock is active, the button becomes non-interactable.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class ButtonLocker : MonoBehaviour
    {
        private Button _button;
        private HashSet<string> _activeLocks = new HashSet<string>();
        
        private void Awake()
        {
            _button = GetComponent<Button>();
        }
        
        /// <summary>
        /// Adds a lock with the specified ID to the button.
        /// </summary>
        /// <param name="lockId">Unique identifier for the lock</param>
        public void AddLock(string lockId)
        {
            if (string.IsNullOrEmpty(lockId))
            {
                Debug.LogWarning("ButtonLocker: Attempted to add a lock with an empty or null ID");
                return;
            }
            
            _activeLocks.Add(lockId);
            UpdateButtonInteractability();
        }
        
        /// <summary>
        /// Removes a lock with the specified ID from the button.
        /// </summary>
        /// <param name="lockId">Unique identifier for the lock to remove</param>
        public void RemoveLock(string lockId)
        {
            if (string.IsNullOrEmpty(lockId))
            {
                Debug.LogWarning("ButtonLocker: Attempted to remove a lock with an empty or null ID");
                return;
            }
            
            _activeLocks.Remove(lockId);
            UpdateButtonInteractability();
        }
        
        /// <summary>
        /// Removes all locks from the button.
        /// </summary>
        public void RemoveAllLocks()
        {
            _activeLocks.Clear();
            UpdateButtonInteractability();
        }
        
        /// <summary>
        /// Checks if a specific lock is active.
        /// </summary>
        /// <param name="lockId">Unique identifier for the lock to check</param>
        /// <returns>True if the lock is active, false otherwise</returns>
        public bool IsLocked(string lockId)
        {
            return _activeLocks.Contains(lockId);
        }
        
        /// <summary>
        /// Checks if the button has any active locks.
        /// </summary>
        /// <returns>True if any locks are active, false otherwise</returns>
        public bool HasAnyLocks()
        {
            return _activeLocks.Count > 0;
        }
        
        private void UpdateButtonInteractability()
        {
            _button.interactable = _activeLocks.Count == 0;
        }
    }
} 