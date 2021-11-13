"use strict";

let connection = new signalR.HubConnectionBuilder().withUrl("/gamehub").build();

let canvas = document.getElementById("game-canvas")
let playerIdDiv = document.getElementById("player")

let gameData = JSON.parse(localStorage.getItem("game"));

connection.on("ReceiveMessage", function (gameState) {

    console.log("ReceiveMessage", gameState)
    draw(gameState)
});

connection.on("Connected", function (data) {
    console.log("Connected", gameData)
    gameData = data
    localStorage.setItem("game", JSON.stringify(data));
 
});

connection.start().then(function () {
    console.log("connected...")

    connection.invoke("Connected").catch(function (err) {
        return console.error(err.toString());
    });
}).catch(function (err) {
    return console.error(err.toString());
});

//document.getElementById("sendButton").addEventListener("click", function (event) {
//    var user = document.getElementById("userInput").value;
//    var message = document.getElementById("messageInput").value;
//    connection.invoke("SendMessage", user, message).catch(function (err) {
//        return console.error(err.toString());
//    });
//    event.preventDefault();
//});

function draw(gameState) {
    let playerId = gameData.playerData.id; 
    playerIdDiv.innerHTML = "Player " + playerId;

    let width = gameData.layout.width * 10;
    let height = gameData.layout.height * 10;

    canvas.width = width
    canvas.height = height

    let context = canvas.getContext('2d');

    // Get a random color, red or blue
    let randomColor = Math.random() > 0.5 ? '#ff8081' : '#0099b0';

    // Draw a rectangle
    context.fillStyle = randomColor;
    context.fillRect(100, 50, 200, 175);




    
}