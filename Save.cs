#region usingStatements
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Storage;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using System.Xml.Serialization;
using System.IO;
using System.IO.IsolatedStorage;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
//using Microsoft.Xna.Framework.Content;
//using TextureAtlas;
//using Projectile;
//using Weapon;
#endregion

namespace Storage
{
    [Serializable]
    public struct SavedGame
    {
        public int CurrentLevel;
        public string Name;
        public DateTime date;
        //public Vector2 playerPosition;
        //[NonSerialized]
        //Don't Keep
        //public string DontKeep;
    }

    public class GameSaver
    {
        static string saveName;
        public GameSaver()
        {
            saveName = @"SaveGame.xml";
        }

        public SavedGame Load()
        {
            SavedGame theSave;
            try
            {
                IsolatedStorageFile isf = System.IO.IsolatedStorage.IsolatedStorageFile.GetStore(IsolatedStorageScope.User | IsolatedStorageScope.Assembly, null, null);
                if (isf.FileExists( saveName))
                {
                    Stream reader = new IsolatedStorageFileStream(saveName, FileMode.Open, isf);
                    IFormatter formatter = new BinaryFormatter();
                    theSave = (SavedGame)formatter.Deserialize(reader);
                    reader.Close();
                }
                else
                    theSave = new SavedGame();
            }
            catch (Exception e)
            {
                throw new ApplicationException("Save Game Failed", e);
            }
            return theSave;
        }

        public void Save(SavedGame theSave)
        {
            try{
                IsolatedStorageFile isf = System.IO.IsolatedStorage.IsolatedStorageFile.GetStore(IsolatedStorageScope.User | 
                    IsolatedStorageScope.Assembly, null, null);
                if (isf.FileExists(saveName))
                {
                    isf.DeleteFile(saveName);
                }
                Stream writer = new IsolatedStorageFileStream(saveName, FileMode.Create, isf);
                IFormatter formatter = new BinaryFormatter();
                formatter.Serialize(writer, theSave);
                writer.Close();
            }
            catch (Exception e)
            {
                throw new ApplicationException("Save Game Failed", e);
            }

        }
    }
}
