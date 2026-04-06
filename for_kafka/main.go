package main

import (
	"bytes"
	"context"
	"crypto/rand"
	"encoding/json"
	"flag"
	"fmt"
	"io"
	"math/big"
	"net"
	"net/http"
	"strconv"
	"time"

	"github.com/segmentio/kafka-go"
)

type OrderStruct struct {
	Fid  string `json:"id1"`
	Sid  string `json:"id2"`
	Time string `json:"time_str"`
}

// sum, avg, min/max, count, collect_list/collect_set, topk
// window tumbling (SIZE X SECONDS/MINUTES/HOURS/DAYS) - по времени, фиксированные
// window hopping - скользящие (size 5 minuets advance by 1) - окно в 5 минут но которое может каждый раз передвигать на 1
// window session - типа умное окно, которое начнёт работать с первым пришедшим в него сообщением, если долго ждать, то закроется
// есть ещё grace period который заставляет не закрывать окно какое-то время после истечения окна
// HAVING - если что-то больше, то

// CREATE TABLE user_product_cancels_set
// >WITH (key_format='JSON')
// >AS
// >  SELECT
// >    user_id,
// >    COLLECT_SET(product) AS total_cancels
// >  FROM canc_ord_transform
// >  GROUP BY user_id
// >  EMIT CHANGES;

//  CREATE TABLE user_product_cancels_hourly
// >WITH (key_format='JSON')
// >AS
// >  SELECT
// >    user_id,
// >    product,
// >    COUNT(*) AS total_cancels
// >  FROM canc_ord_transform
// >  WINDOW TUMBLING (SIZE 5 MINUTES)
// >  GROUP BY user_id, product
// >  EMIT CHANGES;

// CREATE TABLE canc_printers_price
// >WITH (KEY_FORMAT='JSON')
// >AS
// >SELECT
// >    user_id,
// >    product,
// >    COUNT(*) AS total_cancelation
// >FROM canc_ord_transform
// >GROUP BY user_id, product
// >EMIT CHANGES;

type OrderDlQ struct {
	Smth string `json:"smth"`
}

// для ksql:
// так как обысно используется лишь верхний регистр, то если что-то в маленьком регистре оборачивать в ``
// create stream canceled (`order_id` bigint, `who_created` varchar, `user_id` bigint, `product` varchar) with ( KAFKA_TOPIC='order_canceled', value_format='JSON');
// create table canceled_users as select userid, latest_by_offset(id) AS usID from second_orders_canceled group by userid;

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
	topics := []string{"order_shown", "order_viewed", "order_liked", "order_purchaised", "order_bought_together", "order_visited", "orders-dlq"}
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

	topicConfigs := []kafka.TopicConfig{}
	for _, tp := range topics {
		topicConfigs = append(topicConfigs, kafka.TopicConfig{Topic: tp, NumPartitions: 5, ReplicationFactor: 3})
	}

	err = controllerConn.CreateTopics(topicConfigs...)
	if err != nil {
		panic(err.Error())
	}
	go func() {
		how_to_del := "m"
		time_to_sleep := 1 * time.Minute
		time_to_delete := 1
		ttl := strconv.Itoa(time_to_delete) + how_to_del
		body := struct {
			OlderThen string `json:"olderThen"`
		}{
			OlderThen: ttl,
		}
		body_json, _ := json.Marshal(body)
		url := "http://localhost:5000/api/SystemNeo4j/system/edges/old?olderThan=" + strconv.Itoa(time_to_delete) + how_to_del
		url_t := "http://localhost:5000/api/SystemNeo4j/system/nodes/isolated"
		http_client := &http.Client{}
		for true {
			time.Sleep(time_to_sleep)
			req, _ := http.NewRequest("DELETE", url, bytes.NewBuffer(body_json))
			_, err := http_client.Do(req)
			if err != nil {
				// fmt.Println(err.Error())
			}
			n_req, _ := http.NewRequest("DELETE", url_t, nil)
			_, err = http_client.Do(n_req)
			if err != nil {
				// fmt.Println(err.Error())
			}
		}
	}()

	if is_cons == false {
		// producer

		// при включённом автосоздании топиков он сам создатся и мы к нему подключимся и будем писать в него
		// по дефолту эта настройка должна быть включена
		// what_created := []string{"printer", "scaner", "cable", "monitor", "pc", "ic", "acdc"}
		writers := []kafka.Writer{}
		for a, tp := range topics {
			writers = append(writers, kafka.Writer{Addr: kafka.TCP(kafkas...), Topic: tp, WriteTimeout: 10 * time.Second,
				Balancer: &kafka.Hash{}, AllowAutoTopicCreation: true, ReadTimeout: 5 * time.Second, MaxAttempts: 10})
			defer writers[a].Close()
		}
		// order_id := 1

		// stores_ids := []string{"First", "Second", "Third", "Fourth"}
		// __who_purchase := []string{"Ivan", "Makson", "Andrew", "Vladimir", "Vladislav", "Olga", "Eva"} // место в списке = orderID по которому будет ключ
		l := big.NewInt(200)
		m := big.NewInt(100)
		// stores_len := big.NewInt(int64(len(stores_ids)))
		time.Sleep(10000)
		for true {
			t, _ := rand.Int(rand.Reader, m)
			val := t.Int64()
			timestamp := time.Now()
			who, _ := rand.Int(rand.Reader, l)
			what, _ := rand.Int(rand.Reader, l)
			msg := OrderStruct{
				Fid:  who.String(),
				Sid:  what.String(),
				Time: timestamp.Format("2006-01-02 15:04:05"),
			}
			to_payload, _ := json.Marshal(msg)
			if val > 10 {
				message := kafka.Message{
					Key:   []byte(who.String()),
					Value: to_payload,
					Time:  timestamp,
					Headers: []kafka.Header{
						{
							Key:   "eventType",
							Value: []byte("Order_Shown"),
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
				writers[0].WriteMessages(context.TODO(), message)
				fmt.Printf("event: Shown: %s\n", to_payload)
			}
			if val > 17 {
				message := kafka.Message{
					Key:   []byte(who.String()),
					Value: to_payload,
					Time:  timestamp,
					Headers: []kafka.Header{
						{
							Key:   "eventType",
							Value: []byte("Order_Viewed"),
						},
						{
							Key:   "eventID",
							Value: []byte("5"),
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
				writers[1].WriteMessages(context.TODO(), message)
				fmt.Printf("event: Viewed: %s\n", to_payload)
			}
			if val > 34 {
				message := kafka.Message{
					Key:   []byte(who.String()),
					Value: to_payload,
					Time:  timestamp,
					Headers: []kafka.Header{
						{
							Key:   "eventType",
							Value: []byte("Order_Liked"),
						},
						{
							Key:   "eventID",
							Value: []byte("2"),
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
				writers[2].WriteMessages(context.TODO(), message)
				fmt.Printf("event: Liked: %s\n", to_payload)
			}
			if val > 61 {
				message := kafka.Message{
					Key:   []byte(who.String()),
					Value: to_payload,
					Time:  timestamp,
					Headers: []kafka.Header{
						{
							Key:   "eventType",
							Value: []byte("Order_Purchaised"),
						},
						{
							Key:   "eventID",
							Value: []byte("3"),
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
				writers[3].WriteMessages(context.TODO(), message)
				fmt.Printf("event: Purchaised: %s\n", to_payload)
			}
			if val > 78 {
				new_what := what
				for new_what == what {
					new_what, _ = rand.Int(rand.Reader, big.NewInt(25))
				}
				msg := OrderStruct{
					Fid:  who.String(),
					Sid:  what.String(),
					Time: timestamp.Format("2006-01-02 15:04:05"),
				}
				to_payload, _ := json.Marshal(msg)
				message := kafka.Message{
					Key:   []byte(who.String()),
					Value: to_payload,
					Time:  timestamp,
					Headers: []kafka.Header{
						{
							Key:   "eventType",
							Value: []byte("Order_Bought-together"),
						},
						{
							Key:   "eventID",
							Value: []byte("4"),
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
				writers[4].WriteMessages(context.TODO(), message)
				fmt.Printf("event: Bought together: %s\n", to_payload)
			}
			if val > 90 {
				where, _ := rand.Int(rand.Reader, big.NewInt(25))
				msg = OrderStruct{
					Fid:  who.String(),
					Sid:  where.String(),
					Time: timestamp.Format("2006-01-02 15:04:05"),
				}
				to_payload, _ := json.Marshal(msg)
				message := kafka.Message{
					Key:   []byte(who.String()),
					Value: to_payload,
					Time:  timestamp,
					Headers: []kafka.Header{
						{
							Key:   "eventType",
							Value: []byte("Order_Visited"),
						},
						{
							Key:   "eventID",
							Value: []byte("5"),
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
				writers[5].WriteMessages(context.TODO(), message)
				fmt.Printf("event: Visited: %s\n", to_payload)
			}
			time.Sleep(time.Duration(5000))
			// if t.Cmp(big.NewInt(30)) == -1 {
			// 	payload := OrderCreated{
			// 		OrderID: strconv.Itoa(order_id),
			// 		Amount:  1,
			// 		Who:     who_purchase[who.Int64()],
			// 		UserID:  who.String(),
			// 		Product: what_created[what.Int64()],
			// 	}
			// 	to_write, _ := json.Marshal(payload)
			// msg := kafka.Message{
			// 	Key:   []byte(who.String()),
			// 	Value: to_write,
			// 	Time:  timestamp,
			// 	Headers: []kafka.Header{
			// 		{
			// 			Key:   "eventType",
			// 			Value: []byte("OrderCreated"),
			// 		},
			// 		{
			// 			Key:   "eventID",
			// 			Value: []byte("0"),
			// 		},
			// 		{
			// 			Key:   "entityID",
			// 			Value: who.Bytes(),
			// 		},
			// 		{
			// 			Key:   "Source",
			// 			Value: []byte("from Go"),
			// 		},
			// 		{
			// 			Key:   "version",
			// 			Value: []byte("1"),
			// 		},
			// 		{
			// 			Key:   "timestamp",
			// 			Value: []byte(time.Now().Format(time.RFC3339)),
			// 		},
			// 	},
			// 	}
			// 	err := kafka_w_created.WriteMessages(context.Background(), msg)
			// 	if err != nil {
			// 		log.Fatal("failed to write messages: ", err)
			// 	}
			// 	fmt.Print("Order Created ", payload, "\n")
			// } else if t.Cmp(big.NewInt(60)) == -1 {
			// 	payload := OrderPurchaised{
			// 		ProductID: what.String(),
			// 		UserID:    who.String(),
			// 		Rating:    5,
			// 	}
			// 	to_write, _ := json.Marshal(payload)
			// 	msg := kafka.Message{
			// 		Key:   []byte(who.String()),
			// 		Value: to_write,
			// 		Time:  timestamp,
			// 		Headers: []kafka.Header{
			// 			{
			// 				Key:   "eventType",
			// 				Value: []byte("OrderPurchaised"),
			// 			},
			// 			{
			// 				Key:   "eventID",
			// 				Value: []byte("0"),
			// 			},
			// 			{
			// 				Key:   "entityID",
			// 				Value: who.Bytes(),
			// 			},
			// 			{
			// 				Key:   "Source",
			// 				Value: []byte("from Go"),
			// 			},
			// 			{
			// 				Key:   "version",
			// 				Value: []byte("1"),
			// 			},
			// 			{
			// 				Key:   "timestamp",
			// 				Value: []byte(time.Now().Format(time.RFC3339)),
			// 			},
			// 		},
			// 	}
			// 	kafka_w_paid.WriteMessages(context.TODO(), msg)
			// 	fmt.Print("Order Purchaised ", payload, "\n")
			// } else {
			// 	t, _ := rand.Int(rand.Reader, big.NewInt(10))
			// 	payload := orderCanceled{
			// 		OrderID: order_id,
			// 		Who:     who_purchase[who.Int64()],
			// 		UserID:  int(who.Int64()),
			// 		Product: what_created[what.Int64()],
			// 	}
			// 	if t.Cmp(big.NewInt(6)) == -1 {
			// 		payload := OrderDlQ{
			// 			Smth: "this is an error",
			// 		}
			// 		to_write, _ := json.Marshal(payload)
			// 		msg := kafka.Message{
			// 			Key:   []byte(who.String()),
			// 			Value: to_write,
			// 			Time:  timestamp,
			// 			Headers: []kafka.Header{
			// 				{
			// 					Key:   "eventType",
			// 					Value: []byte("OrderCanceled"),
			// 				},
			// 				{
			// 					Key:   "eventID",
			// 					Value: []byte("0"),
			// 				},
			// 				{
			// 					Key:   "entityID",
			// 					Value: who.Bytes(),
			// 				},
			// 				{
			// 					Key:   "Source",
			// 					Value: []byte("from Go"),
			// 				},
			// 				{
			// 					Key:   "version",
			// 					Value: []byte("1"),
			// 				},
			// 				{
			// 					Key:   "timestamp",
			// 					Value: []byte(time.Now().Format(time.RFC3339)),
			// 				},
			// 			},
			// 		}
			// 		kafka_w_canceled.WriteMessages(context.TODO(), msg)
			// 		fmt.Print("Order Canceled ", payload, "\n")
			// 	} else {
			// 		to_write, _ := json.Marshal(payload)
			// 		msg := kafka.Message{
			// 			Key:   []byte(who.String()),
			// 			Value: to_write,
			// 			Time:  timestamp,
			// 			Headers: []kafka.Header{
			// 				{
			// 					Key:   "eventType",
			// 					Value: []byte("OrderCanceled"),
			// 				},
			// 				{
			// 					Key:   "eventID",
			// 					Value: []byte("0"),
			// 				},
			// 				{
			// 					Key:   "entityID",
			// 					Value: who.Bytes(),
			// 				},
			// 				{
			// 					Key:   "Source",
			// 					Value: []byte("from Go"),
			// 				},
			// 				{
			// 					Key:   "version",
			// 					Value: []byte("1"),
			// 				},
			// 				{
			// 					Key:   "timestamp",
			// 					Value: []byte(time.Now().Format(time.RFC3339)),
			// 				},
			// 			},
			// 		}
			// 		kafka_w_canceled.WriteMessages(context.TODO(), msg)
			// 		fmt.Print("Order Canceled ", payload, "\n")
			// 	}
			// }
		}
	} else {
		r_cfg := kafka.ReaderConfig{Brokers: kafkas, GroupID: group_id, Topic: topics[tpc], StartOffset: ofst}
		kafka_dlq := &kafka.Writer{Addr: kafka.TCP(kafkas...), Topic: "orders-dlq", WriteTimeout: 10 * time.Second,
			Balancer: &kafka.Hash{}, AllowAutoTopicCreation: true, ReadTimeout: 5 * time.Second, MaxAttempts: 10}
		defer kafka_dlq.Close()
		kafka_r := kafka.NewReader(r_cfg)
		for true {
			// Fetch+Commit -> больше контроля за тем что мы отсмотрели
			// msg, err := kafka_r.FetchMessage(context.TODO())
			// if err != nil {
			// 	kafka_r.CommitMessages(context.TODO(), msg)
			// }
			if kafka_r.Lag() > 15 {
				msg, err := kafka_r.ReadLag(context.TODO())
				if err != nil {
					if err == io.EOF {
						return
					} else {
						fmt.Errorf("error occured: %s", err.Error())
					}
				}
				fmt.Printf("kafka lag above 15 and is: %d\n", msg)
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
					kafka_dlq.WriteMessages(context.TODO(), msg)
				}
			}
			kafka_r.CommitMessages(context.TODO(), msg)
			fmt.Printf("kafka read message %s\n", msg.Value)
		}
	}
}
