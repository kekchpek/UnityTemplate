using System;
using System.Collections.Generic;
using UnityEngine;
using UnityMVVM.ViewModelCore;

namespace kekchpek.Auxiliary.Contexts
{
    public class ViewModelContext<T> : IDisposable
    {

        private readonly List<IContextedViewModel<T>> _contextedViewModels =
            UnityEngine.Pool.ListPool<IContextedViewModel<T>>.Get();

        private readonly List<IContextSelectorViewModel<T>> _contextSelectors =
            UnityEngine.Pool.ListPool<IContextSelectorViewModel<T>>.Get();

        private readonly List<IViewModel> _coveredViewModels =
            UnityEngine.Pool.ListPool<IViewModel>.Get();

        private T _currentContext;

        public ViewModelContext(IViewModel contextViewModel, T initialContext)
        {
            _currentContext = initialContext;
            HandleViewModel(contextViewModel);
            foreach (var viewModel in GetSubviewsRecursively<IViewModel>(contextViewModel))
            {
                HandleViewModel(viewModel);
            }
        }

        private void HandleViewModel(IViewModel viewModel)
        {
            if (viewModel is IContextedViewModel<T> contextedViewModel)
            {
                _contextedViewModels.Add(contextedViewModel);
                contextedViewModel.SetContext(_currentContext);
            }

            if (viewModel is IContextSelectorViewModel<T> contextSelectorViewModel)
            {
                _contextSelectors.Add(contextSelectorViewModel);
                contextSelectorViewModel.ContextSelected += SetContext;
            }

            viewModel.SubviewCreated += HandleSubviewCreated;
            viewModel.Destroyed += ReleaseViewModel;
            _coveredViewModels.Add(viewModel);
        }

        private void HandleSubviewCreated(IViewModel parent, IViewModel createdSubview)
        {
            HandleViewModel(createdSubview);
        }

        private IEnumerable<TViewModel> GetSubviewsRecursively<TViewModel>(IViewModel viewModel) 
            where TViewModel : IViewModel
        {
            foreach (var childViewModel in viewModel.GetSubviews<TViewModel>())
            {
                yield return childViewModel;
                foreach (var vm in GetSubviewsRecursively<TViewModel>(childViewModel))
                {
                    yield return vm;
                }
            }
        }

        public void SetContext(T context)
        {
            _currentContext = context;
            if (_coveredViewModels.Count == 0)
            {
                Debug.LogError("ViewModelContext does not cover any view models");
            }
            foreach (var contextedViewModel in _contextedViewModels)
            {
                contextedViewModel.SetContext(_currentContext);
            }
        }

        private void ReleaseViewModel(IViewModel viewModel)
        {
            ReleaseViewModel(viewModel, true);
        }

        private void ReleaseViewModel(IViewModel viewModel, bool removeFromCoveredViewModels)
        {

            if (viewModel is IContextSelectorViewModel<T> contextSelectorViewModel)
            {
                _contextSelectors.Add(contextSelectorViewModel);
                contextSelectorViewModel.ContextSelected -= SetContext;
            }

            if (viewModel is IContextedViewModel<T> contextedViewModel)
            {
                _contextedViewModels.Remove(contextedViewModel);
            }

            viewModel.Destroyed -= ReleaseViewModel;
            viewModel.SubviewCreated -= HandleSubviewCreated;

            if (removeFromCoveredViewModels)
            {
                _coveredViewModels.Remove(viewModel);
            }
        }

        public void Dispose()
        {
            foreach (var areaViewModel in _coveredViewModels)
            {
                // Do not remove from covered list because we're iterating over it at the moment.
                ReleaseViewModel(areaViewModel, false);
            }

            // Collections will cleared automatically by UnityEngine.Pool
            UnityEngine.Pool.ListPool<IContextedViewModel<T>>.Release(_contextedViewModels);
            UnityEngine.Pool.ListPool<IContextSelectorViewModel<T>>.Release(_contextSelectors);
            UnityEngine.Pool.ListPool<IViewModel>.Release(_coveredViewModels);
        }
    }
}