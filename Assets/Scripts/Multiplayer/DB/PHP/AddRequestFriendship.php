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
    
    $existingFriendship = R::findOne('friendrequest', 
    'user_id = ? AND friend_id = ?', 
    [$friendId, $userId]
    );
    
    if ($existingFriendship) {
        echo 'already requested';
        exit;
    }

    $friendrequest = R::dispense('friendrequest');

    $friendrequest -> user_id = $friendId;
    $friendrequest -> friend_id = $userId;

    R::store($friendrequest);

    echo $friend_id;
?>