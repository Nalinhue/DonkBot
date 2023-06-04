# DonkBot The DiscordBot
DonkBot is a music bot written in C# using the [DSharpPlus](https://github.com/DSharpPlus/DSharpPlus) library.  
Playing music from youtube using [LavaLink](https://github.com/lavalink-devs/Lavalink) based on the title of the last song played, utilizing 
[YoutubeAPIV3](https://console.cloud.google.com/apis/library/youtube.googleapis.com).

## Prerequisites
[DiscordBot](https://discord.com/developers/applications)  
[LavaLinkServers](https://lavalink.darrennathanael.com/) (either public or self-hosted using the [LavaLinkImage](https://github.com/lavalink-devs/Lavalink))  
[YoutubeAPIV3](https://console.cloud.google.com/apis/library/youtube.googleapis.com) - Sign in, create a project, get a key, enable YouTube Data API v3  
[Docker](https://www.docker.com/) installed

## Installation
### Use my provided image [nalinhue/donkbot:latest](https://hub.docker.com/repository/docker/nalinhue/donkbot/general)  
1. Download and edit the [docker-compose.yml](https://github.com/Nalinhue/DonkBot/blob/main/docker-compose.yml) (use nalinhue/donkbot:latest)  
2. Run the DockerContainer: `docker-compose up`

### Or create your own  
1. Clone the repo: `git clone https://github.com/Nalinhue/DonkBot.git`  
2. Create the DockerImage: `docker build --tag YourImageName:YourTag .`  
3. Edit the [docker-compose.yml](https://github.com/Nalinhue/DonkBot/blob/main/docker-compose.yml) renember to use YourImageName:YourTag  
4. Run the DockerContainer: `docker-compose up`

## Commands
- `-help` or `-h`: List commands
- `-play` or `-p`: Play a song from youtube (doesn't support playlists yet)  
- `-skip` or `-s`: Skip to the next song, use `-skip "QueueIndex"` to skip a certain track in queue    
- `-cease` or `-c`: Stop playing completely  
- `-queue` or `-q`: List the queue

## Fluff
Very open to questions, input, complaints and contributions
### Contact
Nalinhue@gmail.com
### License

Distributed under the MIT License. See `LICENSE` for more information.