<?php

require 'database.php';
    
    $userId = $_POST['id'];
    $friendId = $_POST['friendId'];


    if(isset($userId) == false || isset($friendId) == false){
        echo 'data struct error';
        exit;
    }

        R::exec(
        'DELETE FROM friendrequest WHERE user_id = ? AND friend_id = ?',
        [$userId, $friendId]
        );

        R::exec(
        'DELETE FROM friendrequest WHERE user_id = ? AND friend_id = ?',
        [$friendId, $userId]
        );
?>