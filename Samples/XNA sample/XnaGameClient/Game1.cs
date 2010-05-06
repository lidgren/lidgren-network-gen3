using System;
using System.Collections.Generic;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

using Lidgren.Network;

namespace XnaGameClient
{
	/// <summary>
	/// This is the main type for your game
	/// </summary>
	public class Game1 : Microsoft.Xna.Framework.Game
	{
		GraphicsDeviceManager graphics;
		SpriteBatch spriteBatch;

		Texture2D[] textures;
		Dictionary<long, Vector2> positions = new Dictionary<long, Vector2>();
		NetClient client;

		public Game1()
		{
			graphics = new GraphicsDeviceManager(this);
			Content.RootDirectory = "Content";

			NetPeerConfiguration config = new NetPeerConfiguration("xnaapp");
			config.EnableMessageType(NetIncomingMessageType.DiscoveryResponse);

			client = new NetClient(config);
			client.Start();
		}

		protected override void Initialize()
		{
			client.DiscoverLocalPeers(14242);
			base.Initialize();
		}

		protected override void LoadContent()
		{
			spriteBatch = new SpriteBatch(GraphicsDevice);
			textures = new Texture2D[5];
			for (int i = 0; i < 5; i++)
				textures[i] = Content.Load<Texture2D>("c" + (i + 1));
		}

		protected override void Update(GameTime gameTime)
		{
			//
			// Collect input
			//
			int xinput = 0;
			int yinput = 0;
			KeyboardState keyState = Keyboard.GetState();

			// exit game if escape or Back is pressed
			if (keyState.IsKeyDown(Keys.Escape) || GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
				this.Exit();

			// use arrows or dpad to move avatar
			if (GamePad.GetState(PlayerIndex.One).DPad.Left == ButtonState.Pressed || keyState.IsKeyDown(Keys.Left))
				xinput = -1;
			if (GamePad.GetState(PlayerIndex.One).DPad.Right == ButtonState.Pressed || keyState.IsKeyDown(Keys.Right))
				xinput = 1;
			if (GamePad.GetState(PlayerIndex.One).DPad.Up == ButtonState.Pressed || keyState.IsKeyDown(Keys.Up))
				yinput = -1;
			if (GamePad.GetState(PlayerIndex.One).DPad.Down == ButtonState.Pressed || keyState.IsKeyDown(Keys.Down))
				yinput = 1;

			if (xinput != 0 || yinput != 0)
			{
				//
				// If there's input; send it to server
				//
				NetOutgoingMessage om = client.CreateMessage();
				om.Write(xinput); // very inefficient to send a full Int32 (4 bytes) but we'll use this for simplicity
				om.Write(yinput);
				client.SendMessage(om, NetDeliveryMethod.Unreliable);
			}

			// read messages
			NetIncomingMessage msg;
			while ((msg = client.ReadMessage()) != null)
			{
				switch (msg.MessageType)
				{
					case NetIncomingMessageType.DiscoveryResponse:
						// just connect to first server discovered
						client.Connect(msg.SenderEndpoint);
						break;
					case NetIncomingMessageType.Data:
						// server sent a position update
						long who = msg.ReadInt64();
						int x = msg.ReadInt32();
						int y = msg.ReadInt32();
						positions[who] = new Vector2(x, y);
						break;
				}
			}

			base.Update(gameTime);
		}

		protected override void Draw(GameTime gameTime)
		{
			GraphicsDevice.Clear(Color.CornflowerBlue);

			spriteBatch.Begin(SpriteBlendMode.AlphaBlend);

			// draw all players
			foreach (long who in positions.Keys)
			{
				// use player unique identifier to choose an image
				int num = (int)Math.Abs(who) % 5;

				// draw player
				spriteBatch.Draw(textures[num], positions[who], Color.White);
			}

			spriteBatch.End();

			base.Draw(gameTime);
		}

		protected override void OnExiting(object sender, EventArgs args)
		{
			client.Shutdown("bye");

			base.OnExiting(sender, args);
		}
	}
}
