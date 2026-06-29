<?php

require 'database.php';
    
    $userId = $_POST['id'];
    $friendName = $_POST['login'];


    if(isset($userId) == false || isset($friendName) == false){
        echo 'data struct error';
        exit;
    }

    $friend = R::findOne('users', 'login = ?', array($friendName));

    if(isset($friend) == false){
    echo 'Login error';
    exit;
    }

    $friendId = $friend['id'];

    $existingFriendship = R::findOne('friendships', 
    'user_id = ? AND friend_id = ?', 
    [$userId, $friendId]
    );
    
    if ($existingFriendship) {
        echo 'already friend';
        exit;
    }
    $friendship = R::dispense('friendships');

    $friendship -> user_id = $userId;
    $friendship -> friend_id = $friendId;

    R::store($friendship);

    $friendship = R::dispense('friendships');
    
    $friendship -> user_id = $friendId;
    $friendship -> friend_id = $userId;

    R::store($friendship);

    echo $friend_id;
?>