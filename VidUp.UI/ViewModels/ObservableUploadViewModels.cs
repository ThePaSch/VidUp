﻿#region

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using Drexel.VidUp.Business;

#endregion

namespace Drexel.VidUp.UI.ViewModels
{
    class ObservableUploadViewModels : INotifyCollectionChanged, IEnumerable<UploadViewModel>
    {
        private List<UploadViewModel> uploadViewModels;
        public event NotifyCollectionChangedEventHandler CollectionChanged;
        private ObservableTemplateViewModels observableTemplateViewModels;

        public ObservableUploadViewModels(UploadList uploadList, ObservableTemplateViewModels observableTemplateViewModels)
        {
            this.observableTemplateViewModels = observableTemplateViewModels;
            this.uploadViewModels = new List<UploadViewModel>();
            
            if (uploadList != null)
            {
                foreach (Upload upload in uploadList)
                {
                    this.uploadViewModels.Add(new UploadViewModel(upload, this.observableTemplateViewModels));
                }

                uploadList.CollectionChanged += uploadListCollectionChanged;
            }
        }

        private void uploadListCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                List<UploadViewModel> newViewModels = new List<UploadViewModel>();
                foreach (Upload upload in e.NewItems)
                {
                    newViewModels.Add(new UploadViewModel(upload, this.observableTemplateViewModels));
                }

                this.uploadViewModels.AddRange(newViewModels);

                this.raiseNotifyCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, newViewModels));
                return;
            }

            if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                //if multiple view models removed, remove every view model with a single call, as WPF/MVVM only supports
                //multiple deletes in one call when they are all in direct sequence in the collection
                foreach (Upload upload in e.OldItems)
                {
                    UploadViewModel oldViewModel = this.uploadViewModels.Find(viewModel => viewModel.Upload == upload);
                    int index = this.uploadViewModels.IndexOf(oldViewModel);
                    this.uploadViewModels.Remove(oldViewModel);
                    this.raiseNotifyCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, oldViewModel, index));
                }

                return;
            }

            throw new InvalidOperationException("ObservableTemplateViewModels supports only adding and removing.");
        }

        private void raiseNotifyCollectionChanged(NotifyCollectionChangedEventArgs args)
        {
            NotifyCollectionChangedEventHandler handler = this.CollectionChanged; 
            if (handler != null)
            {
                handler(this, args);
            }
        }

        public IEnumerator<UploadViewModel> GetEnumerator()
        {
            return this.uploadViewModels.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        public void AddUploads(List<Upload> uploads)
        {
            List<UploadViewModel> newViewModels = new List<UploadViewModel>();
            foreach(Upload upload in uploads)
            {
                newViewModels.Add(new UploadViewModel(upload, this.observableTemplateViewModels));
            }

            this.uploadViewModels.AddRange(newViewModels);
            this.raiseNotifyCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, newViewModels));
        }

        public void Remove(Upload upload)
        {
            int position = this.uploadViewModels.FindIndex(uvm => uvm.Guid == upload.Guid.ToString());
            UploadViewModel uploadViewModel = this.uploadViewModels[position];
            this.uploadViewModels.Remove(uploadViewModel);
            this.raiseNotifyCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, uploadViewModel, position));
        }

        public UploadViewModel GetUploadByGuid(Guid guid)
        {
            return this.uploadViewModels.Find(uploadviewModel => uploadviewModel.Guid == guid.ToString());
        }

        public void ReOrder(UploadList uploadList)
        {
            List <UploadViewModel> reOrderedViewModels = new List<UploadViewModel>();
            foreach (Upload upload in uploadList)
            {
                reOrderedViewModels.Add(uploadViewModels.Find(vm => vm.Upload.Guid == upload.Guid));
            }

            this.uploadViewModels = reOrderedViewModels;

            this.raiseNotifyCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }
    }
}
