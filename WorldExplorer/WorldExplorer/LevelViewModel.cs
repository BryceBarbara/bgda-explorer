﻿/*  Copyright (C) 2012 Ian Brown

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media.Media3D;
using System.ComponentModel;
using WorldExplorer.DataLoaders;
using System.Windows.Media.Imaging;
using WorldExplorer.DataModel;
using System.Windows.Media;

namespace WorldExplorer
{
    public class LevelViewModel : INotifyPropertyChanged
    {

        private WriteableBitmap _texture;

        public WriteableBitmap Texture
        {
            get { return _texture; }
            set
            {
                _texture = value;
            }
        }

        private Model _vifModel;

        public Model VifModel
        {
            get { return _vifModel; }
            set
            {
                _vifModel = value;
                this.OnPropertyChanged("VifModel");
            }
        }

        private String _infoText;

        public String InfoText
        {
            get { return _infoText; }
            set
            {
                _infoText = value;
                this.OnPropertyChanged("InfoText");
            }
        }

        private void BuildEmptyScene()
        {
            List<ModelVisual3D> scene = new List<ModelVisual3D>();
            ModelVisual3D ambientLight = new ModelVisual3D();
            ambientLight.Content = new AmbientLight(Color.FromRgb(0x80, 0x80, 0x80));
            scene.Add(ambientLight);
            ModelVisual3D directionalLight = new ModelVisual3D();
            directionalLight.Content = new DirectionalLight(Color.FromRgb(0x80, 0x80, 0x80), new Vector3D(0, -1, -1));
            scene.Add(directionalLight);

            Scene = scene;
        }

        private List<ModelVisual3D> _scene;

        public List<ModelVisual3D> Scene
        {
            get { return _scene; }
            set
            {
                _scene = value;
                this.OnPropertyChanged("Scene");
            }
        }

        private Transform3D _cameraTransform;

        public Transform3D CameraTransform
        {
            get { return _cameraTransform; }
            set
            {
                _cameraTransform = value;
                _camera.Transform = _cameraTransform;
                this.OnPropertyChanged("CameraTransform");
            }
        }

        private Camera _camera = new OrthographicCamera { Position = new Point3D(0, 10, -10), LookDirection = new Vector3D(0, -1, 1) };

        public Camera Camera
        {
            get { return _camera; }
            set
            {
                _camera = value;
                this.OnPropertyChanged("Camera");
            }
        }

        private void UpdateCamera(Rect3D bounds)
        {
            OrthographicCamera oCam = (OrthographicCamera)_camera;

            Point3D centroid = new Point3D(0, 0, 0);
            double radius = Math.Sqrt(bounds.SizeX * bounds.SizeX + bounds.SizeY * bounds.SizeY + bounds.SizeZ * bounds.SizeZ) / 2.0;
            double cameraDistance = radius * 3.0;

            Point3D camPos = new Point3D(centroid.X, centroid.Y - cameraDistance, centroid.Z);
            oCam.Position = camPos;
            oCam.Width = cameraDistance;
            oCam.LookDirection = new Vector3D(0, 1, 0);
            oCam.UpDirection = new Vector3D(0, 0, 1);
        }


        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            if (this.PropertyChanged != null)
                this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion // INotifyPropertyChanged Members
    }
}
