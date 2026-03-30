package main

import (
	"context"
	"crypto/rand"
	"encoding/json"
	"flag"
	"fmt"
	"io"
	"log"
	"math/big"
	"net"
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

type orderCanceled struct {
	OrderID string `json:"order_id"`
	Who     string `json:"who_created"`
	UserID  string `json:"user_id"`
	Product string `json:"product"`
}

type OrderPurchaised struct {
	Who    string `json:"who_created"`
	UserID string `json:"user_id"`
}

// запуск
// ./main -consumer=true -group abc, обязательно у -consumer должно стоять равно иначе произойдёт невкусное
// запускаем короче просто main
// потом пишем для каждого нового
// ./main -consumer=true -group *вставьте имя группы* -topic=[0:2]
func main() {
	var is_cons bool
	var group_id string
	var ofst int64
	var tpc int
	topics := []string{"order-created", "order-purchased", "order-canceled"}
	flag.BoolVar(&is_cons, "consumer", false, "set consumer or producer mode")
	flag.StringVar(&group_id, "group", "first", "set group id. Only use in consumer mode")
	flag.Int64Var(&ofst, "ofset", 0, "set ofset of reading topic, use only in consumer mode")
	flag.IntVar(&tpc, "topic", 0, "set one of three topics, only numbers [0:2]")
	flag.Parse()
	if tpc > 2 || tpc < 0 {
		tpc = 0
	}
	kafkas := []string{"localhost:9092", "localhost:9093", "localhost:9094"}
	conn, err := kafka.Dial("tcp", "localhost:9092")
	if err != nil {
		panic(err.Error())
	}
	defer conn.Close()

	controller, err := conn.Controller()
	if err != nil {
		panic(err.Error())
	}
	controllerConn, err := kafka.Dial("tcp", net.JoinHostPort(controller.Host, strconv.Itoa(controller.Port)))
	if err != nil {
		panic(err.Error())
	}
	defer controllerConn.Close()

	topicConfigs := []kafka.TopicConfig{{Topic: "order-created", NumPartitions: 5, ReplicationFactor: 3},
		{Topic: "order-canceled", NumPartitions: 5, ReplicationFactor: 3}, {Topic: "reder-purchaised", NumPartitions: 5, ReplicationFactor: 3}}

	err = controllerConn.CreateTopics(topicConfigs...)
	if err != nil {
		panic(err.Error())
	}

	if is_cons == false {
		// producer

		// при включённом автосоздании топиков он сам создатся и мы к нему подключимся и будем писать в него
		// по дефолту эта настройка должна быть включена
		what_created := []string{"printer", "scaner", "cable", "monitor", "pc", "ic", "acdc"}
		order_id := 1
		kafka_w_f := &kafka.Writer{Addr: kafka.TCP(kafkas...), Topic: topics[0], WriteTimeout: 10 * time.Second,
			Balancer: &kafka.Hash{}, AllowAutoTopicCreation: true, ReadTimeout: 5 * time.Second, MaxAttempts: 10}
		kafka_w_s := &kafka.Writer{Addr: kafka.TCP(kafkas...), Topic: topics[1], WriteTimeout: 10 * time.Second,
			Balancer: &kafka.Hash{}, AllowAutoTopicCreation: true, ReadTimeout: 5 * time.Second, MaxAttempts: 10}
		kafka_w_t := &kafka.Writer{Addr: kafka.TCP(kafkas...), Topic: topics[2], WriteTimeout: 10 * time.Second,
			Balancer: &kafka.Hash{}, AllowAutoTopicCreation: true, ReadTimeout: 5 * time.Second, MaxAttempts: 10}

		defer kafka_w_f.Close()
		who_purchase := []string{"Ivan", "Makson", "Andrew", "Vladimir", "Vladislav", "Olga", "Eva"} // место в списке = orderID по которому будет ключ
		l := big.NewInt(int64(len(who_purchase)))
		m := big.NewInt(100)
		time.Sleep(10000)
		for true {
			t, _ := rand.Int(rand.Reader, m)
			timestamp := time.Now()
			who, _ := rand.Int(rand.Reader, l)
			what, _ := rand.Int(rand.Reader, l)
			if t.Cmp(big.NewInt(30)) == -1 {
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
				err := kafka_w_f.WriteMessages(context.Background(), msg)
				if err != nil {
					log.Fatal("failed to write messages: ", err)
				}
				order_id++
				fmt.Print("Order Created ", payload, "\n")
			} else if t.Cmp(big.NewInt(60)) == -1 {
				payload := OrderPurchaised{
					Who:    who_purchase[who.Int64()],
					UserID: who.String(),
				}
				to_write, _ := json.Marshal(payload)
				msg := kafka.Message{
					Key:   []byte(who.String()),
					Value: to_write,
					Time:  timestamp,
					Headers: []kafka.Header{
						{
							Key:   "eventType",
							Value: []byte("OrderPurchaised"),
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
				kafka_w_s.WriteMessages(context.TODO(), msg)
				fmt.Print("Order Purchaised ", payload, "\n")
			} else {
				payload := orderCanceled{
					Who:     who_purchase[who.Int64()],
					UserID:  who.String(),
					OrderID: strconv.Itoa(order_id),
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
							Value: []byte("OrderCanceled"),
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
				kafka_w_t.WriteMessages(context.TODO(), msg)
				fmt.Print("Order Canceled ", payload, "\n")
			}
			time.Sleep(time.Duration(5000))
		}
	} else {
		r_cfg := kafka.ReaderConfig{Brokers: kafkas, GroupID: group_id, Topic: topics[tpc], StartOffset: ofst}
		kafka_r := kafka.NewReader(r_cfg)
		for true {
			// Fetch+Commit -> больше контроля за тем что мы отсмотрели
			// msg, err := kafka_r.FetchMessage(context.TODO())
			// if err != nil {
			// 	kafka_r.CommitMessages(context.TODO(), msg)
			// }
			for kafka_r.Lag() > 15 {
				msg, err := kafka_r.ReadLag(context.TODO())
				if err != nil {
					if err != io.EOF {
						msg, err = kafka_r.ReadLag(context.TODO())
					} else {
						return
					}
					if err != nil {
						fmt.Errorf("error occured: %s", err.Error())
					}
				}
				fmt.Printf("kafka read lag: %d\n", msg)
			}
			msg, err := kafka_r.ReadMessage(context.TODO())
			if err != nil {
				if err != io.EOF {
					msg, err = kafka_r.ReadMessage(context.TODO())
				} else {
					return
				}
				if err != nil {
					fmt.Errorf("error occured: %s", err.Error())
				}
			}
			fmt.Printf("kafka read message %s\n", msg.Value)
		}
	}
}

// для ksql:
// create stream orders_canceled (id bigint, name varchar, amount int, orderid bigint, userid bigint, product_name varchar) with ( KAFKA_TOPIC='orders_canceled', value_format='JSON');
// create table users as select userid, latest_by_offset(id) AS usID from orders_canceled group by userid;
