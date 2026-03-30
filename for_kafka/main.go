package main

import (
	"context"
	"crypto/rand"
	"encoding/json"
	"fmt"
	"log"
	"math/big"
	"strconv"
	"time"

	"github.com/segmentio/kafka-go"
)

type OrderCreated struct {
	OrderID string `json:"order_id"`
	Amount  int    `json:"amount"`
	Who     string `json:"who_created"`
	UserID  string `json:"user_id"`
	Product string `json:"product"`
}

func main() {
	fmt.Println("abc")
	// producer
	topic := "order-created"
	// при включённом автосоздании топиков он сам создатся и мы к нему подключимся и будем писать в него
	// по дефолту эта настройка должна быть включена
	kafkas := []string{"localhost:9092", "localhost:9093", "localhost:9094"}
	what_created := []string{"printer", "scaner", "cable", "monitor", "pc", "ic", "acdc"}
	order_id := 1
	kafka_w := &kafka.Writer{Addr: kafka.TCP(kafkas...), Topic: topic, WriteTimeout: 10 * time.Second,
		Balancer: &kafka.Hash{}, AllowAutoTopicCreation: true, ReadTimeout: 5 * time.Second, MaxAttempts: 10}
	defer kafka_w.Close()
	who_purchase := []string{"Ivan", "Makson", "Andrew", "Vladimir", "Vladislav", "Olga", "Eva"} // место в списке = orderID по которому будет ключ
	l := big.NewInt(int64(len(who_purchase)))
	time.Sleep(10000)
	for true {
		timestamp := time.Now()
		who, _ := rand.Int(rand.Reader, l)
		what, _ := rand.Int(rand.Reader, l)
		payload := OrderCreated{
			OrderID: strconv.Itoa(order_id),
			Amount:  1,
			Who:     who_purchase[who.Int64()],
			UserID:  who.String(),
			Product: what_created[what.Int64()],
		}
		to_write, _ := json.Marshal(payload)
		msg := kafka.Message{
			Key:   []byte(who.String()),
			Value: to_write,
			Time:  timestamp,
			Headers: []kafka.Header{
				{
					Key:   "eventType",
					Value: []byte("OrderCreated"),
				},
				{
					Key:   "eventID",
					Value: []byte("0"),
				},
				{
					Key:   "entityID",
					Value: who.Bytes(),
				},
				{
					Key:   "Source",
					Value: []byte("from Go"),
				},
				{
					Key:   "version",
					Value: []byte("1"),
				},
				{
					Key:   "timestamp",
					Value: []byte(time.Now().Format(time.RFC3339)),
				},
			},
		}
		err := kafka_w.WriteMessages(context.Background(), msg)
		if err != nil {
			log.Fatal("failed to write messages: ", err)
		}
		order_id++
		time.Sleep(time.Duration(5000))
		fmt.Println(payload)
	}
	//consumer
	// topic := "my-topica"
	// partition := 1

	// conn, err := kafka.DialLeader(context.Background(), "tcp", "localhost:9092", topic, partition)
	// if err != nil {
	// 	log.Fatal("failed to dial leader:", err)
	// }

	// conn.SetReadDeadline(time.Now().Add(10 * time.Second))
	// batch := conn.ReadBatch(10e3, 1e6) // fetch 10KB min, 1MB max

	// b := make([]byte, 10e3) // 10KB max per message
	// for {
	// 	n, err := batch.Read(b)
	// 	if err != nil {
	// 		break
	// 	}
	// 	fmt.Println(string(b[:n]))
	// }

	// if err := batch.Close(); err != nil {
	// 	log.Fatal("failed to close batch:", err)
	// }

	// if err := conn.Close(); err != nil {
	// 	log.Fatal("failed to close connection:", err)
	// }

	// if auto.create.topics.enabled=false:
	// topic := "my-topic"

	// conn, err := kafka.Dial("tcp", "localhost:9092")
	// if err != nil {
	// 	panic(err.Error())
	// }
	// defer conn.Close()

	// controller, err := conn.Controller()
	// if err != nil {
	// 	panic(err.Error())
	// }
	// var controllerConn *kafka.Conn
	// controllerConn, err = kafka.Dial("tcp", net.JoinHostPort(controller.Host, strconv.Itoa(controller.Port)))
	// if err != nil {
	// 	panic(err.Error())
	// }
	// defer controllerConn.Close()

	// topicConfigs := []kafka.TopicConfig{
	// 	{
	// 		Topic:             topic,
	// 		NumPartitions:     1,
	// 		ReplicationFactor: 1,
	// 	},
	// }

	// err = controllerConn.CreateTopics(topicConfigs...)
	// if err != nil {
	// 	panic(err.Error())
	// }
}
