﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace MusicPlayer {

    public class MusicFile {

        public string Filepath { get; private set; }
        public string Extension => Path.GetExtension(Filepath);

        private TagLib.File MetadataFile { get; set; }
        public int Duration => (int) MetadataFile.Properties.Duration.TotalSeconds;

        public string Title => MetadataFile.Tag.Title ?? Path.GetFileNameWithoutExtension(Filepath);
        public string Artist => MetadataFile.Tag.Performers?.FirstOrDefault() ?? "";
        public string Album => MetadataFile.Tag.Album ?? "";
        public string[] Genres => MetadataFile.Tag.Genres ?? new string[]{};

        public delegate void OnFilePlayed(MusicFile playedFile);
        public static event OnFilePlayed FilePlayed;

        //These extensions will be recognized as valid music files
        public static string[] ValidExtensions { get; } = {
            ".mp3", ".m4a", ".ogg", ".wav", ".flv", ".wmv", ".ink", ".Ink", ".flac"
        };

        public static MusicFile FromFile(string path) {
            if (IsShortcut(path)) {
                path = ResolveShortcut(path);
            }
            
            if(!File.Exists(path) || !ValidExtensions.Contains(Path.GetExtension(path)?.ToLower())) { return null; }
            MusicFile mf = new MusicFile() {
                Filepath = path
            };

            mf.MetadataFile = TagLib.File.Create(mf.Filepath);
            return mf;
        }

        public static async Task<MusicFile> FromFileAsync(string path) {
            if(!File.Exists(path)) { return null; }
            bool isShortcut = IsShortcut(path);

            if (isShortcut) {
                path = ResolveShortcut(path);
            }

            MusicFile mf = new MusicFile() {
                Filepath = path
            };

            mf.MetadataFile = await Task.Run(()=> TagLib.File.Create(mf.Filepath));
            return mf;
        }

        private MusicFile() {
            
        }

        public async Task PlayAsync() {

            await Task.Run(() => {
                Process p = new Process() {
                    StartInfo = new ProcessStartInfo() {
                        WindowStyle = ProcessWindowStyle.Minimized,
                        FileName = Filepath
                    }
                };
                p.Start();
            });

            FilePlayed?.Invoke(this);
        }

        //Checks to see if a file is a shortcut (.Ink extension)
        private static bool IsShortcut(string path) {
            string directory = Path.GetDirectoryName(path);
            string file = Path.GetFileName(path);
            
            bool returnValue = false;

            Thread thr = new Thread(new ThreadStart(() => {
                Shell32.Shell shell = new Shell32.Shell();
                Shell32.Folder folder = shell.NameSpace(directory);
                Shell32.FolderItem folderItem = folder.ParseName(file);

                returnValue = folderItem?.IsLink ?? false;
            }));
            thr.SetApartmentState(ApartmentState.STA);
            thr.Start();
            thr.Join();

            return returnValue;
        }
        //Get the real path of a shortcut file
        private static string ResolveShortcut(string path) {
            string directory = Path.GetDirectoryName(path);
            string file = Path.GetFileName(path);

            string linkPath = file;

            Thread thr = new Thread(new ThreadStart(() => {
                Shell32.Shell shell = new Shell32.Shell();
                Shell32.Folder folder = shell.NameSpace(directory);
                Shell32.FolderItem folderItem = folder.ParseName(file);

                linkPath = ((Shell32.ShellLinkObject) folderItem.GetLink).Path;
            }));
            thr.SetApartmentState(ApartmentState.STA);
            thr.Start();
            thr.Join();
                
            return linkPath;
        }

    }

}