﻿using Drexel.VidUp.Business;
using Drexel.VidUp.JSON;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Windows.Forms;
using System.Windows.Media.Imaging;

namespace Drexel.VidUp.UI.ViewModels
{

    public class TemplateViewModel : INotifyPropertyChanged
    {
        private Template template;
        public event PropertyChangedEventHandler PropertyChanged;
        private MainWindowViewModel mainWindowViewModel;
        private QuarterHourViewModels quarterHourViewModels;
        private GenericCommand openFileDialogCommand;
        private string lastThumbnailFallbackFilePathAdded = null;
        private string lastImageFilePathAdded;

        #region properties

        public Template Template
        {
            get
            {
                return this.template;
            }
            set
            {
                if (this.template != value)
                {
                    this.template = value;
                    //all properties changed
                    RaisePropertyChanged(null);
                }
            }
        }

        public GenericCommand OpenFileDialogCommand
        {
            get
            {
                return this.openFileDialogCommand;
            }
        }
        public string Guid
        { 
            get => this.template != null ? this.template.Guid.ToString() : string.Empty; 
        }
        public DateTime Created
        { 
            get => this.template != null ? this.template.Created : DateTime.MinValue; 
        }
        public DateTime LastModified 
        { 
            get => this.template != null ? this.template.LastModified : DateTime.MinValue; 
        }
        public string Name 
        { 
            get => this.template != null ? this.template.Name : null;
            set
            {
                this.template.Name = value;
                RaisePropertyChangedAndSerializeTemplateList("Name");
            }
        }

        public string Title 
        { 
            get => this.template != null ? this.template.Title : null;
            set
            {
                this.template.Title = value;
                RaisePropertyChangedAndSerializeTemplateList("YtTitle");

            }
        }
        public string Description 
        { 
            get => this.template != null ? this.template.Description : null;
            set
            {
                this.template.Description = value;
                RaisePropertyChangedAndSerializeTemplateList("YtDescription");

            }
        }

        public string TagsAsString 
        { 
            get => this.template != null ? string.Join(",", this.template.Tags) : null;
            set
            {
                this.template.Tags = new List<string>(value.Split(','));
                RaisePropertyChangedAndSerializeTemplateList("TagsAsString");
            }
        }
        public Visibility Visibility 
        { 
            get => this.template != null ? this.template.YtVisibility : Visibility.Private;
            set
            {
                this.template.YtVisibility = value;
                RaisePropertyChangedAndSerializeTemplateList("YtVisibility");
            }
        }

        public Array Visibilities
        {
            get
            {
                return Enum.GetValues(typeof(Visibility));
            }
        }

        /*public string GameTitle 
        { 
            get => this.template != null ? this.template.GameTitle : null;
            set
            {
                this.template.GameTitle = value;
                RaisePropertyChangedAndSerializeTemplateList("GameTitle");
            }
        }*/

        public QuarterHourViewModels QuarterHourViewModels
        {
            get
            {
                return this.quarterHourViewModels;
            }
        }

        public QuarterHourViewModel DefaultPublishAtTime
        {
            get => this.template != null ? this.quarterHourViewModels.GetQuarterHourViewModel(this.template.DefaultPublishAtTime) : this.quarterHourViewModels.GetQuarterHourViewModel(new DateTime(1, 1, 1, 0, 0, 0));
        }



        public BitmapImage ImageBitmap
        {
            get
            {
                if(this.template != null && File.Exists(this.template.ImageFilePathForRendering))
                {
                    BitmapImage bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(this.template.ImageFilePathForRendering, UriKind.Absolute);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();

                    return bitmap;
                }

                return null;
            }
        }

        public string ImageFilePathForEditing
        {
            get => this.template != null ? this.template.ImageFilePathForEditing : null;
            set
            {
                //care this RaisePropertyChanged must take place immediately to show rename hint correctly.
                this.lastImageFilePathAdded = value;
                RaisePropertyChanged("LastImageFilePathAdded");

                string newFilePath = this.mainWindowViewModel.CopyImageToStorageFolder(value, Settings.TemplateImageFolder);
                string oldFilePath = this.template.ImageFilePathForEditing;
                this.template.ImageFilePathForEditing = newFilePath;
                this.mainWindowViewModel.DeleteTemplateImageIfPossible(oldFilePath);
                
                RaisePropertyChanged("ImageBitmap");
                RaisePropertyChangedAndSerializeTemplateList("ImageFilePathForEditing");
            }
        }

        public string LastImageFilePathAdded
        {
            get => this.lastImageFilePathAdded;
        }
        public string RootFolderPath 
        { 
            get => this.template != null ? this.template.RootFolderPath : null;
            set
            {
                this.template.RootFolderPath = value;
                RaisePropertyChangedAndSerializeTemplateList("RootFolderPath");
            }
        }

        public string ThumbnailFolderPath
        {
            get => this.template != null ? this.template.ThumbnailFolderPath : null;
            set
            {
                this.template.ThumbnailFolderPath = value;
                RaisePropertyChangedAndSerializeTemplateList("ThumbnailFolderPath");
            }
        }

        public string ThumbnailFallbackFilePath
        {
            get => this.template != null ? this.template.ThumbnailFallbackFilePath : null;
            set
            {
                //care this RaisePropertyChanged must take place immediately to show rename hint correctly.
                this.lastThumbnailFallbackFilePathAdded = value;
                RaisePropertyChanged("LastThumbnailFallbackFilePathAdded");

                string filePath = this.mainWindowViewModel.CopyImageToStorageFolder(value, Settings.ThumbnailFallbackImageFolder);
                string oldFilePath = this.template.ThumbnailFallbackFilePath;
                this.template.ThumbnailFallbackFilePath = filePath;
                this.mainWindowViewModel.DeleteThumbnailIfPossible(oldFilePath);

                RaisePropertyChangedAndSerializeTemplateList("ThumbnailFallbackFilePath");
            }
        }

        public string LastThumbnailFallbackFilePathAdded
        {
            get => this.lastThumbnailFallbackFilePathAdded;
        }

        public bool IsDefault
        {
            get => this.template != null ? this.template.IsDefault : false;
            set
            {
                this.mainWindowViewModel.SetDefaultTemplate(this.template, value);
                RaisePropertyChangedAndSerializeTemplateList("IsDefault");
            }
        }
        #endregion properties

        public TemplateViewModel(MainWindowViewModel mainWindowViewModel)
        {
            this.mainWindowViewModel = mainWindowViewModel;
            this.openFileDialogCommand = new GenericCommand(openFileDialog);
            this.quarterHourViewModels = new QuarterHourViewModels();
        }

        public void SetDefaultPublishAtTime(DateTime publishAtTime)
        {
            this.template.DefaultPublishAtTime = publishAtTime;
            this.SerializeTemplateList();
        }
        private void RaisePropertyChanged(string propertyName)
        {
            // take a copy to prevent thread issues
            PropertyChangedEventHandler handler = PropertyChanged;
            if (PropertyChanged != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private void openFileDialog(object parameter)
        {
            if ((string)parameter == "pic")
            {
                OpenFileDialog fileDialog = new OpenFileDialog();
                DialogResult result = fileDialog.ShowDialog();

                if (result == DialogResult.OK)
                {
                    this.ImageFilePathForEditing = fileDialog.FileName;
                }
            }

            if ((string)parameter == "root")
            {
                FolderBrowserDialog folderDialog = new FolderBrowserDialog();
                DialogResult result = folderDialog.ShowDialog();

                if (result == DialogResult.OK)
                {
                    this.RootFolderPath = folderDialog.SelectedPath;
                }
            }

            if ((string)parameter == "thumb")
            {
                FolderBrowserDialog folderDialog = new FolderBrowserDialog();
                DialogResult result = folderDialog.ShowDialog();

                if (result == DialogResult.OK)
                {
                    this.ThumbnailFolderPath = folderDialog.SelectedPath;
                }
            }

            if ((string)parameter == "thumbfallback")
            {
                OpenFileDialog fileDialog = new OpenFileDialog();
                DialogResult result = fileDialog.ShowDialog();

                if (result == DialogResult.OK)
                {
                    this.ThumbnailFallbackFilePath = fileDialog.FileName;
                }
            }
        }


        private void RaisePropertyChangedAndSerializeTemplateList(string propertyName)
        {
            this.RaisePropertyChanged(propertyName);
            this.SerializeTemplateList();
        }

        public void SerializeTemplateList()
        {
            JsonSerialization.SerializeTemplateList();
        }
    }
}
