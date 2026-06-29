<?php

require 'database.php';
    
    $userId = $_POST['id'];

    if(isset($userId) == false){
    echo 'data struct error';
    exit;
    }

    $friends = R::getAll(
        'SELECT u.id, u.login 
         FROM friendships f 
         JOIN users u ON f.friend_id = u.id 
         WHERE f.user_id = ?',
        [$userId]
    );
    
    echo json_encode([
    'success' => true,
    'friends' => $friends,
    'count' => count($friends)
    ]);
?>